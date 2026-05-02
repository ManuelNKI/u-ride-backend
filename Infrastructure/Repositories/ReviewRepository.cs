using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(Guid id)
        => await _context.Reviews.FindAsync(id);

    public async Task<List<Review>> GetByTripIdAsync(Guid tripId)
        => await _context.Reviews
            .Where(r => r.TripId == tripId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<Review>> GetReceivedByUserAsync(string toUid)
        => await _context.Reviews
            .Where(r => r.ToUid == toUid)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task AddAsync(Review review)
        => await _context.Reviews.AddAsync(review);
}
