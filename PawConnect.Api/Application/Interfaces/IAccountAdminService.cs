using PawConnect.Api.Application.DTOs.Admin;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.Interfaces;

public interface IAccountAdminService
{
    Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUsersQuery query, CancellationToken cancellationToken);

    Task SetStatusAsync(Guid adminId, Guid userId, AccountStatus status, CancellationToken cancellationToken);
}