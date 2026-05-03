namespace Application.DTOs.Trips;

/// <summary>
/// DTO para actualizar un viaje existente (solo el conductor).
/// </summary>
public class UpdateTripDto
{
    public string? RouteName { get; set; }
    public string? PaymentMethod { get; set; }
    public string? OriginZone { get; set; }
    public string? DestinationZone { get; set; }
    public double? OriginLat { get; set; }
    public double? OriginLng { get; set; }
    public double? DestinationLat { get; set; }
    public double? DestinationLng { get; set; }
    public DateTime? DepartureAt { get; set; }
    public int? SeatsTotal { get; set; }
    public int? SeatsAvailable { get; set; }
    public decimal? Price { get; set; }
    public string? Notes { get; set; }
    public VehicleInfoDto? Vehicle { get; set; }
    public TripRulesDto? Rules { get; set; }
}
