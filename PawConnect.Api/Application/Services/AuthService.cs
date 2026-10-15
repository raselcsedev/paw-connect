using Microsoft.AspNetCore.Identity;
using PawConnect.Api.Application.DTOs.Auth;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;

namespace PawConnect.Api.Application.Services;

public class AuthService : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";

    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserManager<ApplicationUser> users, ITokenService tokens, ILogger<AuthService> logger)
    {
        _users = users;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
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
        return BuildResponse(user, request.Role);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
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
        return BuildResponse(user, role);
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

    private async Task<string> GetRoleAsync(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? Roles.Adopter;
    }

    private AuthResponse BuildResponse(ApplicationUser user, string role)
    {
        var (token, expiresAtUtc) = _tokens.CreateAccessToken(user, role);
        return new AuthResponse(token, expiresAtUtc, ToUserDto(user, role));
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