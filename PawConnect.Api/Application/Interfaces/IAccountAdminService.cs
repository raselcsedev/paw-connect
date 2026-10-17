using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.Interfaces;

public interface IAccountAdminService
{
    Task SetStatusAsync(Guid adminId, Guid userId, AccountStatus status, CancellationToken cancellationToken);
}