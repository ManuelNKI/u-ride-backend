using Application.DTOs.Notifications;

namespace Application.Services;

public interface INotificationService
{
    Task<List<NotificationDto>> GetByUserAsync(string userUid);
    Task<int> GetUnreadCountAsync(string userUid);
    Task MarkAsReadAsync(Guid notificationId, string userUid);
    Task SendNotificationAsync(string userUid, string title, string message,
        Domain.Enums.NotificationType type, Guid? tripId = null,
        string? driverUid = null, string? driverName = null);
}
