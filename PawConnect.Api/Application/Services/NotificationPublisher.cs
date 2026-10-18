using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class NotificationPublisher : INotificationPublisher
{
    private readonly AppDbContext _db;

    public NotificationPublisher(AppDbContext db)
    {
        _db = db;
    }

    public void Publish(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        AdoptionApplication? application = null,
        int? petId = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = recipientId,
            Type = type,
            Title = title,
            Message = message,
            Application = application,
            PetId = application?.PetId ?? petId
        });
    }
}