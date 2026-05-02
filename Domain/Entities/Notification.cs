using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Notificación in-app para un usuario.
/// </summary>
public class Notification : AuditableEntity
{
    public Guid Id { get; set; }

    // ──── Destinatario ────
    public string UserUid { get; set; } = null!;

    // ──── Contenido ────
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; }

    // ──── Contexto opcional del viaje ────
    public Guid? TripId { get; set; }
    public string? DriverUid { get; set; }
    public string? DriverName { get; set; }

    // ──── Estado ────
    public bool Read { get; set; }

    // ──── Navegación ────
    public User User { get; set; } = null!;
}
