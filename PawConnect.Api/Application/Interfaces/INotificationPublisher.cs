using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.Interfaces;

public interface INotificationPublisher
{
    /// <summary>
    /// Adds a notification to the current unit of work. It does NOT save:
    /// the caller's SaveChangesAsync commits it together with the business change.
    /// </summary>
    void Publish(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        AdoptionApplication? application = null,
        int? petId = null);
}