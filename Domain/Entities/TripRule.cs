namespace Domain.Entities;

/// <summary>
/// Regla de convivencia global (administrada por admins).
/// Ejemplo: "Puntualidad", "No compartir datos sensibles"
/// </summary>
public class TripRule : AuditableEntity
{
    public Guid Id { get; set; }
    public string Text { get; set; } = null!;
}
