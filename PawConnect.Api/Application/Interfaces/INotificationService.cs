using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Notifications;

namespace PawConnect.Api.Application.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetMineAsync(
        Guid userId, NotificationQuery query, CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    Task MarkReadAsync(int id, Guid userId, CancellationToken cancellationToken);

    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);
}