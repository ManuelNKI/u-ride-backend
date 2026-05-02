using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Solicitud de un pasajero para unirse a un viaje.
/// Al aceptarse, se reduce SeatsAvailable del Trip de forma transaccional.
/// </summary>
public class TripRequest : AuditableEntity
{
    public Guid Id { get; set; }

    // ──── Referencias ────
    public Guid TripId { get; set; }
    public string PassengerUid { get; set; } = null!;
    public string PassengerName { get; set; } = null!;

    // ──── Estado ────
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    // ──── Banderas post-viaje ────
    public bool DriverRated { get; set; }
    public bool DriverReported { get; set; }

    // ──── Navegación ────
    public Trip Trip { get; set; } = null!;
    public User Passenger { get; set; } = null!;
}
