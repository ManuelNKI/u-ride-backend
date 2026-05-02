using Application.DTOs.TripRequests;

namespace Application.Services;

public interface ITripRequestService
{
    Task<TripRequestDto> CreateRequestAsync(string passengerUid, string passengerName, CreateTripRequestDto dto);
    Task<TripRequestDto> AcceptRequestAsync(Guid requestId, string driverUid);
    Task<TripRequestDto> RejectRequestAsync(Guid requestId, string driverUid);
    Task<TripRequestDto> CancelRequestAsync(Guid requestId, string passengerUid);
    Task<List<TripRequestDto>> GetByTripIdAsync(Guid tripId);
    Task<List<TripRequestDto>> GetByPassengerAsync(string passengerUid);
}
