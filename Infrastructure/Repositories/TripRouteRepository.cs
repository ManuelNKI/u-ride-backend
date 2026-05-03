using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TripRouteRepository : ITripRouteRepository
{
    private readonly ApplicationDbContext _context;

    public TripRouteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TripRoute>> GetAllAsync()
        => await _context.TripRoutes.OrderBy(r => r.Name).AsNoTracking().ToListAsync();

    public async Task<TripRoute?> GetByIdAsync(Guid id)
        => await _context.TripRoutes.FindAsync(id);

    public async Task<bool> ExistsByNameAsync(string name)
        => await _context.TripRoutes.AnyAsync(r => r.Name == name);

    public async Task AddAsync(TripRoute route)
        => await _context.TripRoutes.AddAsync(route);

    public void Update(TripRoute route)
        => _context.TripRoutes.Update(route);

    public void Delete(TripRoute route)
        => _context.TripRoutes.Remove(route);
}
