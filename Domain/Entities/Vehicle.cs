namespace Domain.Entities;

/// <summary>
/// Vehículo registrado por un conductor.
/// </summary>
public class Vehicle : AuditableEntity
{
    public int Id { get; set; }

    // ──── Propietario (Conductor) ────
    public string OwnerUid { get; set; } = null!;
    public User Owner { get; set; } = null!;

    // ──── Detalles del vehículo ────
    public string Brand { get; set; } = null!;
    public string ModelOrBusNumber { get; set; } = null!;
    public string Plate { get; set; } = null!;
    public string Color { get; set; } = null!;
    public int Seats { get; set; }
}
