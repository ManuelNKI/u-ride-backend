using Domain.Entities;

namespace Application.Interfaces;

public interface ITripRouteRepository
{
    Task<List<TripRoute>> GetAllAsync();
    Task<TripRoute?> GetByIdAsync(Guid id);
    Task<bool> ExistsByNameAsync(string name);
    Task AddAsync(TripRoute route);
    void Update(TripRoute route);
    void Delete(TripRoute route);
}
