namespace Application.DTOs.Trips;

/// <summary>
/// DTO para crear un nuevo viaje. Incluye vehículo y reglas.
/// </summary>
public class CreateTripDto
{
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
    public decimal Price { get; set; }
    public string? Notes { get; set; }

    // Vehículo
    public VehicleInfoDto Vehicle { get; set; } = null!;

    // Reglas
    public List<string>? RuleTexts { get; set; }
    public TripRulesDto Rules { get; set; } = null!;
}

public class VehicleInfoDto
{
    public string Plate { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Color { get; set; } = null!;
}

public class TripRulesDto
{
    public bool Punctuality { get; set; }
    public bool Respect { get; set; }
    public bool NoSensitiveData { get; set; }
}
