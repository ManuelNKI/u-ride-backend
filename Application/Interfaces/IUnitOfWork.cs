namespace Application.Interfaces;

/// <summary>
/// Unit of Work — agrupa todos los repositorios y
/// expone SaveChangesAsync para commits transaccionales.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ITripRepository Trips { get; }
    ITripRequestRepository TripRequests { get; }
    IReviewRepository Reviews { get; }
    IReportRepository Reports { get; }
    INotificationRepository Notifications { get; }

    Task<int> SaveChangesAsync();
}
