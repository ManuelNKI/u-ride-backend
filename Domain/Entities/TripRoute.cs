namespace Domain.Entities;

/// <summary>
/// Ruta predefinida para viajes (administrada por admins).
/// Ejemplo: "Izamba - Huachi Chico - Querochaca"
/// </summary>
public class TripRoute : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}
