namespace Application.DTOs.Reports;

/// <summary>
/// DTO de respuesta para un reporte.
/// </summary>
public class ReportDto
{
    public Guid Id { get; set; }
    public string ReporterUid { get; set; } = null!;
    public string ReportedUid { get; set; } = null!;
    public Guid? TripId { get; set; }
    public string Reason { get; set; } = null!;
    public string? EvidenceUrl { get; set; }
    public string Status { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
