using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TripRequestRepository : ITripRequestRepository
{
    private readonly ApplicationDbContext _context;

    public TripRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripRequest?> GetByIdAsync(Guid id)
        => await _context.TripRequests
            .Include(r => r.Trip)
            .Include(r => r.Passenger)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<List<TripRequest>> GetByTripIdAsync(Guid tripId)
        => await _context.TripRequests
            .Where(r => r.TripId == tripId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<TripRequest>> GetByPassengerUidAsync(string passengerUid)
        => await _context.TripRequests
            .Include(r => r.Trip)
            .Where(r => r.PassengerUid == passengerUid)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task AddAsync(TripRequest request)
        => await _context.TripRequests.AddAsync(request);

    public void Update(TripRequest request)
        => _context.TripRequests.Update(request);
}
