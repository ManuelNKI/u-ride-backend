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

        // Notificar a todos los administradores del sistema
        try
        {
            var allUsers = await _uow.Users.GetAllAsync(1, 500);
            var admins = allUsers.Where(u => u.IsAdmin).ToList();
            foreach (var admin in admins)
            {
                await _notificationService.SendNotificationAsync(
                    userUid: admin.FirebaseUid,
                    title: "Nuevo Reporte",
                    message: $"Se ha recibido un nuevo reporte. Motivo: {dto.Reason}",
                    type: NotificationType.System
                );
            }
        }
        catch { /* best-effort */ }

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

                var openReports = await _uow.Reports.GetByReportedUidAsync(report.ReportedUid);
                foreach (var r in openReports.Where(x => x.Id != report.Id && x.Status == ReportStatus.Open))
                {
                    r.Status = ReportStatus.Resolved;
                    r.Action = ReportAction.Suspended;
                    r.AdminNotes = "[Auto-resuelto porque la cuenta fue suspendida en otro reporte]";
                    _uow.Reports.Update(r);
                }
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
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentReports = await _uow.Reports.GetByReportedUidAsync(report.ReportedUid);
            var recentWarnings = recentReports.Count(r => 
                r.Action == ReportAction.Warned && 
                r.UpdatedAt >= thirtyDaysAgo) + 1; // +1 to include this new warning

            if (recentWarnings >= 3)
            {
                // Auto suspend instead
                report.Action = ReportAction.Suspended;
                report.AdminNotes = (adminNotes + " [Auto-suspendido por acumular 3 advertencias en 30 días]").Trim();
                
                var user = await _uow.Users.GetByUidAsync(report.ReportedUid);
                if (user is not null)
                {
                    user.SuspendedUntil = DateTime.UtcNow.AddDays(7);
                    _uow.Users.Update(user);

                    foreach (var r in recentReports.Where(x => x.Id != report.Id && x.Status == ReportStatus.Open))
                    {
                        r.Status = ReportStatus.Resolved;
                        r.Action = ReportAction.Suspended;
                        r.AdminNotes = "[Auto-resuelto porque la cuenta fue suspendida automáticamente por límite de advertencias]";
                        _uow.Reports.Update(r);
                    }
                }

                await _notificationService.SendNotificationAsync(
                    userUid: report.ReportedUid,
                    title: "Cuenta Suspendida",
                    message: $"Has acumulado {recentWarnings} advertencias en el último mes. Tu cuenta ha sido suspendida por 7 días automáticamente.",
                    type: NotificationType.System
                );
            }
            else
            {
                int warningsLeft = 3 - recentWarnings;
                await _notificationService.SendNotificationAsync(
                    userUid: report.ReportedUid,
                    title: "Advertencia",
                    message: $"Has recibido una advertencia debido a un reporte. Acumulas {recentWarnings} advertencia(s) en el último mes. A las 3 advertencias tu cuenta será suspendida (te faltan {warningsLeft}).",
                    type: NotificationType.System
                );
            }
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
