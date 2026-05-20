using Application.DTOs.Vehicles;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class VehicleService : IVehicleService
{
    private readonly ApplicationDbContext _context;

    public VehicleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<VehicleDto>> GetVehiclesByUserAsync(string uid)
    {
        var vehicles = await _context.Vehicles
            .Where(v => v.OwnerUid == uid)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        return vehicles.Select(MapToDto);
    }

    public async Task<VehicleDto> GetVehicleByIdAsync(int id, string uid)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.OwnerUid == uid);

        if (vehicle == null)
            throw new KeyNotFoundException("Vehículo no encontrado.");

        return MapToDto(vehicle);
    }

    public async Task<VehicleDto> CreateVehicleAsync(string uid, CreateVehicleDto dto)
    {
        var vehicle = new Vehicle
        {
            OwnerUid = uid,
            Brand = dto.Brand,
            ModelOrBusNumber = dto.ModelOrBusNumber,
            Plate = dto.Plate,
            Color = dto.Color,
            Seats = dto.Seats
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return MapToDto(vehicle);
    }

    public async Task<VehicleDto> UpdateVehicleAsync(int id, string uid, UpdateVehicleDto dto)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.OwnerUid == uid);

        if (vehicle == null)
            throw new KeyNotFoundException("Vehículo no encontrado.");

        vehicle.Brand = dto.Brand;
        vehicle.ModelOrBusNumber = dto.ModelOrBusNumber;
        vehicle.Plate = dto.Plate;
        vehicle.Color = dto.Color;
        vehicle.Seats = dto.Seats;

        await _context.SaveChangesAsync();

        return MapToDto(vehicle);
    }

    public async Task DeleteVehicleAsync(int id, string uid)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.OwnerUid == uid);

        if (vehicle == null)
            throw new KeyNotFoundException("Vehículo no encontrado.");

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();
    }

    private static VehicleDto MapToDto(Vehicle vehicle)
    {
        return new VehicleDto
        {
            Id = vehicle.Id,
            Brand = vehicle.Brand,
            ModelOrBusNumber = vehicle.ModelOrBusNumber,
            Plate = vehicle.Plate,
            Color = vehicle.Color,
            Seats = vehicle.Seats,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt
        };
    }
}
