namespace Application.DTOs.Reports;

/// <summary>
/// DTO para crear un reporte contra otro usuario.
/// </summary>
public class CreateReportDto
{
    public string ReportedUid { get; set; } = null!;
    public Guid? TripId { get; set; }
    public string Reason { get; set; } = null!;
    public string? EvidenceUrl { get; set; }
}
