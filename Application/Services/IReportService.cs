using Application.DTOs.Common;
using Application.DTOs.Reports;

namespace Application.Services;

public interface IReportService
{
    Task<ReportDto> CreateReportAsync(string reporterUid, CreateReportDto dto);
    Task<PagedResultDto<ReportDto>> GetAllAsync(int page, int pageSize);
    Task<ReportDto> ResolveReportAsync(Guid reportId, string action, string? adminNotes);
}
