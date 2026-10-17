using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class AccountAdminService : IAccountAdminService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;
    private readonly ILogger<AccountAdminService> _logger;

    public AccountAdminService(
        UserManager<ApplicationUser> users,
        AppDbContext db,
        ILogger<AccountAdminService> logger)
    {
        _users = users;
        _db = db;
        _logger = logger;
    }

    public async Task SetStatusAsync(
        Guid adminId, Guid userId, AccountStatus status, CancellationToken cancellationToken)
    {
        if (adminId == userId && status == AccountStatus.Disabled)
        {
            throw new ConflictException("You cannot disable your own account.");
        }

        var user = await _users.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User", userId);

        if (user.Status == status)
        {
            return;
        }

        user.Status = status;

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("The account status could not be updated.");
        }

        if (status == AccountStatus.Disabled)
        {
            await _db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(t => t.RevokedAtUtc, (DateTime?)DateTime.UtcNow),
                    cancellationToken);
        }

        _logger.LogInformation(
            "AUDIT: admin {AdminId} set account {UserId} to {Status}", adminId, userId, status);
    }
}