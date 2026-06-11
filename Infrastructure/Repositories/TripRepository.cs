using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly ApplicationDbContext _context;

    public TripRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Trip?> GetByIdAsync(Guid id)
        => await _context.Trips
            .Include(t => t.Driver)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<Trip>> SearchAsync(
        string? originZone,
        string? destinationZone,
        DateTime? departureDate,
        int page,
        int pageSize)
    {
        var query = BuildSearchQuery(originZone, destinationZone, departureDate);

        return await query
            .OrderBy(t => t.DepartureAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> SearchCountAsync(
        string? originZone,
        string? destinationZone,
        DateTime? departureDate)
    {
        return await BuildSearchQuery(originZone, destinationZone, departureDate).CountAsync();
    }

    public async Task<List<Trip>> GetByDriverUidAsync(string driverUid)
        => await _context.Trips
            .Where(t => t.DriverUid == driverUid)
            .OrderByDescending(t => t.DepartureAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<Trip>> GetActiveByDriverUidAsync(string driverUid)
        => await _context.Trips
            .Where(t => t.DriverUid == driverUid && (t.Status == TripStatus.Open || t.Status == TripStatus.InProgress))
            .OrderBy(t => t.DepartureAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task AddAsync(Trip trip)
        => await _context.Trips.AddAsync(trip);

    public void Update(Trip trip)
        => _context.Trips.Update(trip);

    public void Delete(Trip trip)
        => _context.Trips.Remove(trip);

    // ──── Helper privado para construir la query de búsqueda ────
    private IQueryable<Trip> BuildSearchQuery(
        string? originZone,
        string? destinationZone,
        DateTime? departureDate)
    {
        var query = _context.Trips
            .Where(t => t.Status == TripStatus.Open && t.SeatsAvailable > 0);

        if (!string.IsNullOrWhiteSpace(originZone))
            query = query.Where(t => t.OriginZone.Contains(originZone));

        if (!string.IsNullOrWhiteSpace(destinationZone))
            query = query.Where(t => t.DestinationZone.Contains(destinationZone));

        if (departureDate.HasValue)
            query = query.Where(t => t.DepartureAt.Date == departureDate.Value.Date);

        return query;
    }
}
