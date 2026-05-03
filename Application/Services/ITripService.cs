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
    Task<TripDto> UpdateTripAsync(Guid tripId, string driverUid, UpdateTripDto dto);
    Task DeleteTripAsync(Guid tripId, string driverUid);

    // ──── Rutas admin ────
    Task<List<TripRouteDto>> GetAllRoutesAsync();
    Task<TripRouteDto> CreateRouteAsync(string name);
    Task<TripRouteDto> UpdateRouteAsync(Guid id, string name);
    Task DeleteRouteAsync(Guid id);

    // ──── Reglas admin ────
    Task<List<TripRuleDto>> GetAllRulesAsync();
    Task<TripRuleDto> CreateRuleAsync(string text);
    Task<TripRuleDto> UpdateRuleAsync(Guid id, string text);
    Task DeleteRuleAsync(Guid id);
}

// ──── DTOs para rutas y reglas ────
public class TripRouteDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class TripRuleDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = null!;
}
