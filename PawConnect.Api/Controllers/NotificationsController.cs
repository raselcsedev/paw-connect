using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Notifications;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    /// <summary>The signed-in user's notifications, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetMine(
        [FromQuery] NotificationQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _notifications.GetMineAsync(User.GetUserId(), query, cancellationToken));
    }

    /// <summary>A cheap count for the navigation badge.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _notifications.GetUnreadCountAsync(User.GetUserId(), cancellationToken);
        return Ok(new UnreadCountDto(count));
    }

    [HttpPatch("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        await _notifications.MarkReadAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(typeof(MarkAllReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MarkAllReadDto>> MarkAllRead(CancellationToken cancellationToken)
    {
        var updated = await _notifications.MarkAllReadAsync(User.GetUserId(), cancellationToken);
        return Ok(new MarkAllReadDto(updated));
    }
}