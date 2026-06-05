using Domain.Entities;

namespace Application.Interfaces;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(Guid id);
    Task<List<Report>> GetAllAsync(int page, int pageSize);
    Task<List<Report>> GetByReportedUidAsync(string reportedUid);
    Task<bool> HasReportedForTripAsync(string reporterUid, Guid tripId);
    Task<bool> HasReportedUserForTripAsync(string reporterUid, Guid tripId, string reportedUid);
    Task<int> CountAsync();
    Task AddAsync(Report report);
    void Update(Report report);
}
