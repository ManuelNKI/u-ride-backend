using Application.DTOs.Vehicles;

namespace Application.Interfaces;

public interface IVehicleService
{
    Task<IEnumerable<VehicleDto>> GetVehiclesByUserAsync(string uid);
    Task<VehicleDto> GetVehicleByIdAsync(int id, string uid);
    Task<VehicleDto> CreateVehicleAsync(string uid, CreateVehicleDto dto);
    Task<VehicleDto> UpdateVehicleAsync(int id, string uid, UpdateVehicleDto dto);
    Task DeleteVehicleAsync(int id, string uid);
}
