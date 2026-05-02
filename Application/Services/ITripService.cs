using Application.DTOs.Common;
using Application.DTOs.Trips;

namespace Application.Services;

public interface ITripService
{
    Task<TripDto> CreateTripAsync(string driverUid, string driverName, CreateTripDto dto);
    Task<TripDto?> GetByIdAsync(Guid id);
    Task<PagedResultDto<TripDto>> SearchTripsAsync(TripSearchDto search);
    Task<List<TripDto>> GetDriverTripsAsync(string driverUid);
    Task<TripDto> UpdateStatusAsync(Guid tripId, string driverUid, string newStatus);
}
