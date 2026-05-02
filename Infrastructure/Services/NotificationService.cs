using Application.DTOs.Notifications;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;

    public NotificationService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<NotificationDto>> GetByUserAsync(string userUid)
    {
        var notifications = await _uow.Notifications.GetByUserUidAsync(userUid);
        return notifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(string userUid)
        => await _uow.Notifications.GetUnreadCountAsync(userUid);

    public async Task MarkAsReadAsync(Guid notificationId, string userUid)
    {
        var notifications = await _uow.Notifications.GetByUserUidAsync(userUid);
        var notification = notifications.FirstOrDefault(n => n.Id == notificationId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

        notification.Read = true;
        _uow.Notifications.Update(notification);
        await _uow.SaveChangesAsync();
    }

    public async Task SendNotificationAsync(
        string userUid, string title, string message,
        NotificationType type, Guid? tripId = null,
        string? driverUid = null, string? driverName = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserUid = userUid,
            Title = title,
            Message = message,
            Type = type,
            TripId = tripId,
            DriverUid = driverUid,
            DriverName = driverName,
            Read = false
        };

        await _uow.Notifications.AddAsync(notification);
        await _uow.SaveChangesAsync();
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type.ToString().ToLowerInvariant(),
        TripId = n.TripId,
        DriverUid = n.DriverUid,
        DriverName = n.DriverName,
        Read = n.Read,
        CreatedAt = n.CreatedAt
    };
}
