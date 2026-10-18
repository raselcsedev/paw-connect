using System.ComponentModel.DataAnnotations;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Notifications;

public class NotificationQuery
{
    public bool UnreadOnly { get; set; }

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public record NotificationDto(
    int Id,
    NotificationType Type,
    string Title,
    string Message,
    int? ApplicationId,
    int? PetId,
    bool IsRead,
    DateTime CreatedAtUtc);

public record UnreadCountDto(int Count);

public record MarkAllReadDto(int Updated);