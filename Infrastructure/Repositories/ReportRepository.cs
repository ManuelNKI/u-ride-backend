using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Report?> GetByIdAsync(Guid id)
        => await _context.Reports
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<List<Report>> GetAllAsync(int page, int pageSize)
        => await _context.Reports
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

    public async Task<int> CountAsync()
        => await _context.Reports.CountAsync();

    public async Task AddAsync(Report report)
        => await _context.Reports.AddAsync(report);

    public void Update(Report report)
        => _context.Reports.Update(report);
}
