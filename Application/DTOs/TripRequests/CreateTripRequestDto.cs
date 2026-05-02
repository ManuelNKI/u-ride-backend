namespace Application.DTOs.TripRequests;

/// <summary>
/// DTO para crear una solicitud de viaje.
/// El PassengerUid y PassengerName se extraen del token/claim.
/// </summary>
public class CreateTripRequestDto
{
    public Guid TripId { get; set; }
}
