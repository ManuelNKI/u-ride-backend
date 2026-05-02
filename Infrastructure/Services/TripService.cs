using Application.DTOs.Common;
using Application.DTOs.Trips;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Services;

public class TripService : ITripService
{
    private readonly IUnitOfWork _uow;

    public TripService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<TripDto> CreateTripAsync(string driverUid, string driverName, CreateTripDto dto)
    {
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            DriverUid = driverUid,
            DriverName = driverName,
            RouteName = dto.RouteName,
            PaymentMethod = dto.PaymentMethod,
            OriginZone = dto.OriginZone,
            DestinationZone = dto.DestinationZone,
            OriginLat = dto.OriginLat,
            OriginLng = dto.OriginLng,
            DestinationLat = dto.DestinationLat,
            DestinationLng = dto.DestinationLng,
            DepartureAt = dto.DepartureAt,
            SeatsTotal = dto.SeatsTotal,
            SeatsAvailable = dto.SeatsTotal,
            Price = dto.Price,
            Notes = dto.Notes,
            Status = TripStatus.Open,
            Vehicle = new VehicleInfo
            {
                Plate = dto.Vehicle.Plate,
                Model = dto.Vehicle.Model,
                Brand = dto.Vehicle.Brand,
                Color = dto.Vehicle.Color
            },
            Rules = new TripRules
            {
                Punctuality = dto.Rules.Punctuality,
                Respect = dto.Rules.Respect,
                NoSensitiveData = dto.Rules.NoSensitiveData
            }
        };

        await _uow.Trips.AddAsync(trip);

        // Incrementar conteo de viajes del conductor
        var driver = await _uow.Users.GetByUidAsync(driverUid);
        if (driver is not null)
        {
            driver.DriverTripsCount++;
            driver.TripsCount++;
            _uow.Users.Update(driver);
        }

        await _uow.SaveChangesAsync();
        return MapToDto(trip);
    }

    public async Task<TripDto?> GetByIdAsync(Guid id)
    {
        var trip = await _uow.Trips.GetByIdAsync(id);
        return trip is null ? null : MapToDto(trip);
    }

    public async Task<PagedResultDto<TripDto>> SearchTripsAsync(TripSearchDto search)
    {
        var trips = await _uow.Trips.SearchAsync(
            search.OriginZone,
            search.DestinationZone,
            search.DepartureDate,
            search.Page,
            search.PageSize);

        var count = await _uow.Trips.SearchCountAsync(
            search.OriginZone,
            search.DestinationZone,
            search.DepartureDate);

        return new PagedResultDto<TripDto>
        {
            Items = trips.Select(MapToDto).ToList(),
            TotalCount = count,
            Page = search.Page,
            PageSize = search.PageSize
        };
    }

    public async Task<List<TripDto>> GetDriverTripsAsync(string driverUid)
    {
        var trips = await _uow.Trips.GetByDriverUidAsync(driverUid);
        return trips.Select(MapToDto).ToList();
    }

    public async Task<TripDto> UpdateStatusAsync(Guid tripId, string driverUid, string newStatus)
    {
        var trip = await _uow.Trips.GetByIdAsync(tripId)
            ?? throw new KeyNotFoundException($"Trip {tripId} not found.");

        if (trip.DriverUid != driverUid)
            throw new UnauthorizedAccessException("Only the driver can update trip status.");

        if (!Enum.TryParse<TripStatus>(newStatus, true, out var status))
            throw new ArgumentException($"Invalid trip status: {newStatus}");

        trip.Status = status;
        _uow.Trips.Update(trip);
        await _uow.SaveChangesAsync();

        return MapToDto(trip);
    }

    // ──── Mapping helper ────
    private static TripDto MapToDto(Trip trip) => new()
    {
        Id = trip.Id,
        DriverUid = trip.DriverUid,
        DriverName = trip.DriverName,
        RouteName = trip.RouteName,
        PaymentMethod = trip.PaymentMethod,
        OriginZone = trip.OriginZone,
        DestinationZone = trip.DestinationZone,
        OriginLat = trip.OriginLat,
        OriginLng = trip.OriginLng,
        DestinationLat = trip.DestinationLat,
        DestinationLng = trip.DestinationLng,
        DepartureAt = trip.DepartureAt,
        SeatsTotal = trip.SeatsTotal,
        SeatsAvailable = trip.SeatsAvailable,
        Price = trip.Price,
        Notes = trip.Notes,
        Status = trip.Status.ToString().ToLowerInvariant(),
        ConfirmedPassengerUids = trip.ConfirmedPassengerUids,
        Vehicle = new VehicleInfoDto
        {
            Plate = trip.Vehicle.Plate,
            Model = trip.Vehicle.Model,
            Brand = trip.Vehicle.Brand,
            Color = trip.Vehicle.Color
        },
        Rules = new TripRulesDto
        {
            Punctuality = trip.Rules.Punctuality,
            Respect = trip.Rules.Respect,
            NoSensitiveData = trip.Rules.NoSensitiveData
        },
        CreatedAt = trip.CreatedAt,
        UpdatedAt = trip.UpdatedAt
    };
}
