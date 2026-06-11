using Application.DTOs.Common;
using Application.DTOs.Reports;

namespace Application.Services;

public interface IReportService
{
    Task<ReportDto> CreateReportAsync(string reporterUid, CreateReportDto dto);
    Task<bool> HasReportedForTripAsync(string reporterUid, Guid tripId);
    Task<bool> HasReportedUserForTripAsync(string reporterUid, Guid tripId, string reportedUid);
    Task<PagedResultDto<ReportDto>> GetAllAsync(int page, int pageSize);
    Task<ReportDto> ResolveReportAsync(Guid reportId, string action, string? adminNotes, int? suspensionDays = null);
}
