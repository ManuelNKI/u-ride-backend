using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Reporte de un usuario contra otro. Puede o no estar asociado a un Trip.
/// FKs a User usan Restrict para evitar cascadas circulares.
/// </summary>
public class Report : AuditableEntity
{
    public Guid Id { get; set; }

    // ──── Referencias ────
    public string ReporterUid { get; set; } = null!;
    public string ReportedUid { get; set; } = null!;
    public Guid? TripId { get; set; }

    // ──── Contenido ────
    public string Reason { get; set; } = null!;
    public string? EvidenceUrl { get; set; }

    // ──── Estado y Acción ────
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public ReportAction Action { get; set; } = ReportAction.None;
    public string? AdminNotes { get; set; }

    // ──── Navegación ────
    public User Reporter { get; set; } = null!;
    public User ReportedUser { get; set; } = null!;
    public Trip? Trip { get; set; }
}
