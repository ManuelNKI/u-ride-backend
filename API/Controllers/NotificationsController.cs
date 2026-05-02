using System.Security.Claims;
using Application.DTOs.Notifications;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Obtiene todas las notificaciones del usuario autenticado.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetMyNotifications()
    {
        var uid = GetFirebaseUid();
        var notifications = await _notificationService.GetByUserAsync(uid);
        return Ok(notifications);
    }

    /// <summary>
    /// Obtiene el conteo de notificaciones no leídas.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount()
    {
        var uid = GetFirebaseUid();
        var count = await _notificationService.GetUnreadCountAsync(uid);
        return Ok(new { count });
    }

    /// <summary>
    /// Marca una notificación como leída.
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var uid = GetFirebaseUid();
        await _notificationService.MarkAsReadAsync(id, uid);
        return NoContent();
    }

    private string GetFirebaseUid()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue("user_id")
           ?? throw new UnauthorizedAccessException("Firebase UID not found in token.");
}
