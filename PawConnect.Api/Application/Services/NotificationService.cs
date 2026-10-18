using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Notifications;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<NotificationDto>> GetMineAsync(
        Guid userId, NotificationQuery parameters, CancellationToken cancellationToken)
    {
        IQueryable<Notification> query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (parameters.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        query = query.OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Message, n.ApplicationId, n.PetId, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(items, parameters.Page, parameters.PageSize, totalCount);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task MarkReadAsync(int id, Guid userId, CancellationToken cancellationToken)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Notification", id);

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                s => s.SetProperty(n => n.IsRead, true)
                      .SetProperty(n => n.ReadAtUtc, (DateTime?)now),
                cancellationToken);
    }
}