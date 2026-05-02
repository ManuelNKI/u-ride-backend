namespace Application.DTOs.TripRequests;

/// <summary>
/// DTO de respuesta para una solicitud de viaje.
/// </summary>
public class TripRequestDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public string PassengerUid { get; set; } = null!;
    public string PassengerName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public bool DriverRated { get; set; }
    public bool DriverReported { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
