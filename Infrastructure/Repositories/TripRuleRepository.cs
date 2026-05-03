using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TripRuleRepository : ITripRuleRepository
{
    private readonly ApplicationDbContext _context;

    public TripRuleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TripRule>> GetAllAsync()
        => await _context.TripRules.OrderBy(r => r.Text).AsNoTracking().ToListAsync();

    public async Task<TripRule?> GetByIdAsync(Guid id)
        => await _context.TripRules.FindAsync(id);

    public async Task AddAsync(TripRule rule)
        => await _context.TripRules.AddAsync(rule);

    public void Update(TripRule rule)
        => _context.TripRules.Update(rule);

    public void Delete(TripRule rule)
        => _context.TripRules.Remove(rule);
}
