namespace Domain.Entities;

/// <summary>
/// Calificación entre usuarios después de un viaje.
/// FromUid y ToUid apuntan a User con DeleteBehavior.Restrict
/// para evitar cascadas circulares.
/// </summary>
public class Review : AuditableEntity
{
    public Guid Id { get; set; }

    // ──── Referencias ────
    public Guid TripId { get; set; }
    public string FromUid { get; set; } = null!;
    public string ToUid { get; set; } = null!;

    // ──── Contenido ────
    public int Stars { get; set; }
    public string? Comment { get; set; }

    // ──── Navegación ────
    public Trip Trip { get; set; } = null!;
    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
}
