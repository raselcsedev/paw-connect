using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;
using PawConnect.Api.Application.DTOs.Admin;
using PawConnect.Api.Application.DTOs.Common;

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
        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(
        AdminUsersQuery parameters, CancellationToken cancellationToken)
    {
        var query =
            from u in _db.Users.AsNoTracking()
            join ur in _db.UserRoles on u.Id equals ur.UserId
            join r in _db.Roles on ur.RoleId equals r.Id
            select new { User = u, RoleName = r.Name };

        if (parameters.Status.HasValue)
        {
            var status = parameters.Status.Value;
            query = query.Where(x => x.User.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Role))
        {
            var role = parameters.Role;
            query = query.Where(x => x.RoleName == role);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            var term = parameters.Q.Trim();
            query = query.Where(x =>
                x.User.DisplayName.Contains(term) || (x.User.Email != null && x.User.Email.Contains(term)));
        }

        var ordered = query.OrderByDescending(x => x.User.CreatedAtUtc).ThenBy(x => x.User.Id);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(x => new AdminUserDto(
                x.User.Id,
                x.User.DisplayName,
                x.User.Email ?? string.Empty,
                x.RoleName ?? string.Empty,
                x.User.Status,
                x.User.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserDto>(items, parameters.Page, parameters.PageSize, totalCount);
    }
}