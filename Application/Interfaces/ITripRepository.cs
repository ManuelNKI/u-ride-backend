using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id);
    Task<List<Trip>> SearchAsync(
        string? originZone,
        string? destinationZone,
        DateTime? departureDate,
        int page,
        int pageSize);
    Task<int> SearchCountAsync(
        string? originZone,
        string? destinationZone,
        DateTime? departureDate);
    Task<List<Trip>> GetByDriverUidAsync(string driverUid);
    Task<List<Trip>> GetActiveByDriverUidAsync(string driverUid);
    Task AddAsync(Trip trip);
    void Update(Trip trip);
    void Delete(Trip trip);
}
