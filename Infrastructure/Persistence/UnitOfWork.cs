using Application.Interfaces;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

/// <summary>
/// Unit of Work — agrupa repositorios y coordina transacciones.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IUserRepository Users { get; }
    public ITripRepository Trips { get; }
    public ITripRequestRepository TripRequests { get; }
    public IReviewRepository Reviews { get; }
    public IReportRepository Reports { get; }
    public INotificationRepository Notifications { get; }
    public ITripRouteRepository TripRoutes { get; }
    public ITripRuleRepository TripRules { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Users = new UserRepository(context);
        Trips = new TripRepository(context);
        TripRequests = new TripRequestRepository(context);
        Reviews = new ReviewRepository(context);
        Reports = new ReportRepository(context);
        Notifications = new NotificationRepository(context);
        TripRoutes = new TripRouteRepository(context);
        TripRules = new TripRuleRepository(context);
    }

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public void Dispose()
        => _context.Dispose();
}
