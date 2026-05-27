using Application.DTOs.Common;
using Application.DTOs.Reports;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    public ReportService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
    }

    public async Task<ReportDto> CreateReportAsync(string reporterUid, CreateReportDto dto)
    {
        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReporterUid = reporterUid,
            ReportedUid = dto.ReportedUid,
            TripId = dto.TripId,
            Reason = dto.Reason,
            EvidenceUrl = dto.EvidenceUrl,
            Status = ReportStatus.Open,
            Action = ReportAction.None
        };

        await _uow.Reports.AddAsync(report);
        await _uow.SaveChangesAsync();

        return MapToDto(report);
    }

    public async Task<PagedResultDto<ReportDto>> GetAllAsync(int page, int pageSize)
    {
        var reports = await _uow.Reports.GetAllAsync(page, pageSize);
        var count = await _uow.Reports.CountAsync();

        return new PagedResultDto<ReportDto>
        {
            Items = reports.Select(MapToDto).ToList(),
            TotalCount = count,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Resuelve un reporte, aplica la acción y opcionalmente suspende al usuario.
    /// </summary>
    public async Task<ReportDto> ResolveReportAsync(Guid reportId, string action, string? adminNotes)
    {
        var report = await _uow.Reports.GetByIdAsync(reportId)
            ?? throw new KeyNotFoundException($"Report {reportId} not found.");

        if (!Enum.TryParse<ReportAction>(action, true, out var reportAction))
            throw new ArgumentException($"Invalid action: {action}");

        report.Status = ReportStatus.Resolved;
        report.Action = reportAction;
        report.AdminNotes = adminNotes;
        _uow.Reports.Update(report);

        // Si la acción es suspender, aplicar suspensión al usuario reportado
        if (reportAction == ReportAction.Suspended)
        {
            var user = await _uow.Users.GetByUidAsync(report.ReportedUid);
            if (user is not null)
            {
                user.SuspendedUntil = DateTime.UtcNow.AddDays(7); // Suspensión de 7 días por defecto
                _uow.Users.Update(user);
            }
            
            await _notificationService.SendNotificationAsync(
                userUid: report.ReportedUid,
                title: "Cuenta Suspendida",
                message: "Tu cuenta ha sido suspendida por 7 días debido a un reporte en tu contra.",
                type: NotificationType.System
            );
        }
        else if (reportAction == ReportAction.Warned)
        {
            await _notificationService.SendNotificationAsync(
                userUid: report.ReportedUid,
                title: "Advertencia",
                message: "Has recibido una advertencia debido a un reporte. Por favor, respeta las normas de la comunidad.",
                type: NotificationType.System
            );
        }

        await _uow.SaveChangesAsync();
        return MapToDto(report);
    }

    private static ReportDto MapToDto(Report r) => new()
    {
        Id = r.Id,
        ReporterUid = r.ReporterUid,
        ReportedUid = r.ReportedUid,
        TripId = r.TripId,
        Reason = r.Reason,
        EvidenceUrl = r.EvidenceUrl,
        Status = r.Status.ToString().ToLowerInvariant(),
        Action = r.Action.ToString().ToLowerInvariant(),
        AdminNotes = r.AdminNotes,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
