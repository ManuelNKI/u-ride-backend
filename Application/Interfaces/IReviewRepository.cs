using Domain.Entities;

namespace Application.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id);
    Task<List<Review>> GetByTripIdAsync(Guid tripId);
    Task<List<Review>> GetReceivedByUserAsync(string toUid);
    Task AddAsync(Review review);
}
