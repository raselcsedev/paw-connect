using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Application.DTOs.Auth;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class AuthService : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidRefresh = "The refresh token is not valid.";

    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;
    private readonly AppDbContext _db;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> users,
        ITokenService tokens,
        AppDbContext db,
        ILogger<AuthService> logger)
    {
        _users = users;
        _tokens = tokens;
        _db = db;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        if (await _users.FindByEmailAsync(email) is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            Status = AccountStatus.Active
        };

        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            throw new AppValidationException(ToErrors(created));
        }

        var roleResult = await _users.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);
            throw new InvalidOperationException("The role could not be assigned.");
        }

        _logger.LogInformation("User {UserId} registered as {Role}", user.Id, request.Role);
        return await IssueTokensAsync(user, request.Role, Guid.NewGuid(), cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim());

        if (user is null)
        {
            throw new AuthenticationFailedException(InvalidCredentials);
        }

        if (await _users.IsLockedOutAsync(user))
        {
            throw new AuthenticationFailedException("Too many failed attempts. Try again later.");
        }

        if (!await _users.CheckPasswordAsync(user, request.Password))
        {
            await _users.AccessFailedAsync(user);
            _logger.LogWarning("Failed login for user {UserId}", user.Id);
            throw new AuthenticationFailedException(InvalidCredentials);
        }

        await _users.ResetAccessFailedCountAsync(user);

        if (user.Status == AccountStatus.Disabled)
        {
            throw new ForbiddenException("This account has been disabled.");
        }

        var role = await GetRoleAsync(user);
        _logger.LogInformation("User {UserId} logged in", user.Id);
        return await IssueTokensAsync(user, role, Guid.NewGuid(), cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = _tokens.HashToken(refreshToken);

        var stored = await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new AuthenticationFailedException(InvalidRefresh);

        var now = DateTime.UtcNow;

        if (stored.RevokedAtUtc is not null)
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}; family {FamilyId} revoked",
                stored.UserId, stored.FamilyId);
            throw new AuthenticationFailedException(InvalidRefresh);
        }

        if (stored.ExpiresAtUtc <= now)
        {
            throw new AuthenticationFailedException(InvalidRefresh);
        }

        var user = await _users.FindByIdAsync(stored.UserId.ToString());

        if (user is null)
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            throw new AuthenticationFailedException(InvalidRefresh);
        }

        if (user.Status == AccountStatus.Disabled)
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            throw new ForbiddenException("This account has been disabled.");
        }

        var claimed = await _db.RefreshTokens
            .Where(t => t.Id == stored.Id && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, (DateTime?)now), cancellationToken);

        if (claimed == 0)
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            _logger.LogWarning(
                "Concurrent refresh of one token for user {UserId}; family {FamilyId} revoked",
                stored.UserId, stored.FamilyId);
            throw new AuthenticationFailedException(InvalidRefresh);
        }

        var role = await GetRoleAsync(user);
        return await IssueTokensAsync(user, role, stored.FamilyId, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = _tokens.HashToken(refreshToken);

        var stored = await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            return;
        }

        await RevokeFamilyAsync(stored.FamilyId, DateTime.UtcNow, cancellationToken);
        _logger.LogInformation("User {UserId} logged out", stored.UserId);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _users.FindByIdAsync(userId.ToString());

        if (user is null || user.Status == AccountStatus.Disabled)
        {
            throw new AuthenticationFailedException("The account is not available.");
        }

        return ToUserDto(user, await GetRoleAsync(user));
    }

    private async Task<AuthResponse> IssueTokensAsync(
        ApplicationUser user, string role, Guid familyId, CancellationToken cancellationToken)
    {
        var (accessToken, accessExpiresAtUtc) = _tokens.CreateAccessToken(user, role);
        var refresh = _tokens.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = refresh.Hash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = refresh.ExpiresAtUtc
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            accessExpiresAtUtc,
            refresh.Token,
            refresh.ExpiresAtUtc,
            ToUserDto(user, role));
    }

    private Task<int> RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken)
    {
        return _db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, (DateTime?)now), cancellationToken);
    }

    private async Task<string> GetRoleAsync(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? Roles.Adopter;
    }

    private static UserDto ToUserDto(ApplicationUser user, string role) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, role);

    private static Dictionary<string, string[]> ToErrors(IdentityResult result) =>
        result.Errors
            .GroupBy(e => KeyFor(e.Code))
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

    private static string KeyFor(string code)
    {
        if (code.StartsWith("Password", StringComparison.Ordinal))
        {
            return "Password";
        }

        if (code.Contains("Email", StringComparison.Ordinal) || code.Contains("UserName", StringComparison.Ordinal))
        {
            return "Email";
        }

        return "General";
    }
}