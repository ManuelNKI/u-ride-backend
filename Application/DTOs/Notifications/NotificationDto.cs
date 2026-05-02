namespace Application.DTOs.Notifications;

/// <summary>
/// DTO de respuesta para una notificación.
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;
    public Guid? TripId { get; set; }
    public string? DriverUid { get; set; }
    public string? DriverName { get; set; }
    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; }
}
