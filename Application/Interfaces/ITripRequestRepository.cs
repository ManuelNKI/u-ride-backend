using Domain.Entities;

namespace Application.Interfaces;

public interface ITripRequestRepository
{
    Task<TripRequest?> GetByIdAsync(Guid id);
    Task<List<TripRequest>> GetByTripIdAsync(Guid tripId);
    Task<List<TripRequest>> GetByPassengerUidAsync(string passengerUid);
    Task AddAsync(TripRequest request);
    void Update(TripRequest request);
}
