using Domain.Entities;

namespace Application.Interfaces;

public interface ITripRuleRepository
{
    Task<List<TripRule>> GetAllAsync();
    Task<TripRule?> GetByIdAsync(Guid id);
    Task AddAsync(TripRule rule);
    void Update(TripRule rule);
    void Delete(TripRule rule);
}
