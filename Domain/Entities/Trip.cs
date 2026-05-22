using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Viaje publicado por un conductor.
/// Incluye owned types para VehicleInfo y Rules,
/// y una lista JSON de UIDs de pasajeros confirmados (EF Core 8).
/// </summary>
public class Trip : AuditableEntity
{
    public Guid Id { get; set; }

    // ──── Conductor ────
    public string DriverUid { get; set; } = null!;
    public string DriverName { get; set; } = null!;

    // ──── Ruta ────
    public string RouteName { get; set; } = null!;
    public string OriginZone { get; set; } = null!;
    public string DestinationZone { get; set; } = null!;

    // ──── Coordenadas (opcionales) ────
    public double? OriginLat { get; set; }
    public double? OriginLng { get; set; }
    public double? DestinationLat { get; set; }
    public double? DestinationLng { get; set; }

    // ──── Detalles del viaje ────
    public DateTime DepartureAt { get; set; }
    public int SeatsTotal { get; set; }
    public int SeatsAvailable { get; set; }
    public decimal Price { get; set; }
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public TripStatus Status { get; set; } = TripStatus.Open;

    // ──── Pasajeros confirmados (almacenado como JSON en SQL Server) ────
    public List<string> ConfirmedPassengerUids { get; set; } = new();

    public List<string> RuleTexts { get; set; } = new();

    // ──── Owned Types ────
    public VehicleInfo Vehicle { get; set; } = null!;
    public TripRules Rules { get; set; } = null!;

    // ──── Navegación ────
    public User Driver { get; set; } = null!;
    public ICollection<TripRequest> Requests { get; set; } = new List<TripRequest>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
