namespace Application.DTOs.Trips;

/// <summary>
/// DTO de respuesta para un viaje. Mapeo completo con
/// vehículo, reglas y pasajeros confirmados.
/// </summary>
public class TripDto
{
    public Guid Id { get; set; }
    public string DriverUid { get; set; } = null!;
    public string DriverName { get; set; } = null!;
    public string RouteName { get; set; } = null!;
    public string PaymentMethod { get; set; } = null!;
    public string OriginZone { get; set; } = null!;
    public string DestinationZone { get; set; } = null!;
    public double? OriginLat { get; set; }
    public double? OriginLng { get; set; }
    public double? DestinationLat { get; set; }
    public double? DestinationLng { get; set; }
    public DateTime DepartureAt { get; set; }
    public int SeatsTotal { get; set; }
    public int SeatsAvailable { get; set; }
    public decimal Price { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = null!;
    public List<string> ConfirmedPassengerUids { get; set; } = new();
    public List<string> RuleTexts { get; set; } = new();
    public VehicleInfoDto Vehicle { get; set; } = null!;
    public TripRulesDto Rules { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
