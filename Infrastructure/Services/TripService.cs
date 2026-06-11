using Application.DTOs.Common;
using Application.DTOs.Trips;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

using System.Collections.Concurrent;

namespace Infrastructure.Services;

public class TripService : ITripService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;
    private static readonly ConcurrentDictionary<Guid, DriverLocationDto> _liveLocations = new();

    public TripService(IUnitOfWork uow, INotificationService notifications)
    {
        _uow = uow;
        _notifications = notifications;
    }

    public void SetDriverLiveLocation(Guid tripId, DriverLocationDto location)
    {
        location.UpdatedAt = DateTime.UtcNow;
        _liveLocations[tripId] = location;
    }

    public DriverLocationDto? GetDriverLiveLocation(Guid tripId)
    {
        _liveLocations.TryGetValue(tripId, out var loc);
        return loc;
    }

    public async Task<TripDto> CreateTripAsync(string driverUid, string driverName, CreateTripDto dto)
    {
        var driver = await _uow.Users.GetByUidAsync(driverUid) 
            ?? throw new InvalidOperationException("Driver not found.");

        if (driver.SuspendedUntil.HasValue && driver.SuspendedUntil.Value > DateTime.UtcNow)
        {
            throw new InvalidOperationException($"Tu cuenta está suspendida hasta {driver.SuspendedUntil.Value:dd/MM/yyyy HH:mm} UTC y no puedes publicar viajes.");
        }

        if (driver.Disabled)
        {
            throw new InvalidOperationException("Tu cuenta está desactivada y no puedes publicar viajes.");
        }

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
            RuleTexts = dto.RuleTexts ?? new List<string>(),
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
        driver.DriverTripsCount++;
        driver.TripsCount++;
        _uow.Users.Update(driver);

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

    public async Task<List<TripDto>> GetActiveTripsAsync(string driverUid)
    {
        var trips = await _uow.Trips.GetActiveByDriverUidAsync(driverUid);
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

        if (status == TripStatus.InProgress)
        {
            var driver = await _uow.Users.GetByUidAsync(driverUid);
            if (driver?.SuspendedUntil.HasValue == true && driver.SuspendedUntil.Value > DateTime.UtcNow)
            {
                throw new InvalidOperationException($"Tu cuenta está suspendida hasta {driver.SuspendedUntil.Value:dd/MM/yyyy HH:mm} UTC y no puedes iniciar viajes.");
            }
            if (driver?.Disabled == true)
            {
                throw new InvalidOperationException("Tu cuenta está desactivada y no puedes iniciar viajes.");
            }
        }

        var oldStatus = trip.Status;
        trip.Status = status;
        _uow.Trips.Update(trip);
        await _uow.SaveChangesAsync();

        if (oldStatus != status)
        {
            if (status == TripStatus.InProgress)
            {
                foreach (var uid in trip.ConfirmedPassengerUids)
                {
                    await _notifications.SendNotificationAsync(uid, "Viaje Iniciado", $"El conductor {trip.DriverName} ha iniciado el viaje.", Domain.Enums.NotificationType.TripStarted, trip.Id, trip.DriverUid, trip.DriverName);
                }
            }
            else if (status == TripStatus.Completed)
            {
                foreach (var uid in trip.ConfirmedPassengerUids)
                {
                    await _notifications.SendNotificationAsync(uid, "Viaje Finalizado", $"El viaje con {trip.DriverName} ha terminado. ¡No olvides calificarlo!", Domain.Enums.NotificationType.TripCompleted, trip.Id, trip.DriverUid, trip.DriverName);
                }
            }
            else if (status == TripStatus.Cancelled)
            {
                foreach (var uid in trip.ConfirmedPassengerUids)
                {
                    await _notifications.SendNotificationAsync(uid, "Viaje Cancelado", $"El conductor {trip.DriverName} ha cancelado el viaje.", Domain.Enums.NotificationType.TripCancelled, trip.Id, trip.DriverUid, trip.DriverName);
                }
            }
        }

        return MapToDto(trip);
    }

    public async Task<TripDto> UpdateTripAsync(Guid tripId, string driverUid, UpdateTripDto dto)
    {
        var trip = await _uow.Trips.GetByIdAsync(tripId)
            ?? throw new KeyNotFoundException($"Trip {tripId} not found.");

        if (trip.DriverUid != driverUid)
            throw new UnauthorizedAccessException("Only the driver can update this trip.");

        if (trip.Status != TripStatus.Open)
            throw new InvalidOperationException("Only open trips can be edited.");

        // Actualizar campos si se proporcionan
        if (dto.RouteName is not null) trip.RouteName = dto.RouteName;
        if (dto.PaymentMethod is not null) trip.PaymentMethod = dto.PaymentMethod;
        if (dto.OriginZone is not null) trip.OriginZone = dto.OriginZone;
        if (dto.DestinationZone is not null) trip.DestinationZone = dto.DestinationZone;
        if (dto.OriginLat.HasValue) trip.OriginLat = dto.OriginLat;
        if (dto.OriginLng.HasValue) trip.OriginLng = dto.OriginLng;
        if (dto.DestinationLat.HasValue) trip.DestinationLat = dto.DestinationLat;
        if (dto.DestinationLng.HasValue) trip.DestinationLng = dto.DestinationLng;
        if (dto.DepartureAt.HasValue) trip.DepartureAt = dto.DepartureAt.Value;
        if (dto.SeatsTotal.HasValue) trip.SeatsTotal = dto.SeatsTotal.Value;
        if (dto.SeatsAvailable.HasValue) trip.SeatsAvailable = dto.SeatsAvailable.Value;
        if (dto.Price.HasValue) trip.Price = dto.Price.Value;
        if (dto.Notes is not null) trip.Notes = dto.Notes;
        if (dto.RuleTexts is not null) trip.RuleTexts = dto.RuleTexts;

        if (dto.Vehicle is not null)
        {
            trip.Vehicle = new VehicleInfo
            {
                Plate = dto.Vehicle.Plate,
                Model = dto.Vehicle.Model,
                Brand = dto.Vehicle.Brand,
                Color = dto.Vehicle.Color
            };
        }

        if (dto.Rules is not null)
        {
            trip.Rules = new TripRules
            {
                Punctuality = dto.Rules.Punctuality,
                Respect = dto.Rules.Respect,
                NoSensitiveData = dto.Rules.NoSensitiveData
            };
        }

        _uow.Trips.Update(trip);
        await _uow.SaveChangesAsync();
        return MapToDto(trip);
    }

    public async Task DeleteTripAsync(Guid tripId, string driverUid)
    {
        var trip = await _uow.Trips.GetByIdAsync(tripId)
            ?? throw new KeyNotFoundException($"Trip {tripId} not found.");

        if (trip.DriverUid != driverUid)
            throw new UnauthorizedAccessException("Only the driver can delete this trip.");

        if (trip.Status != TripStatus.Open)
            throw new InvalidOperationException("Only open trips can be deleted.");

        _uow.Trips.Delete(trip);
        await _uow.SaveChangesAsync();
    }

    public async Task CancelFutureTripsForUserAsync(string driverUid)
    {
        var activeTrips = await _uow.Trips.GetActiveByDriverUidAsync(driverUid);
        var futureOpenTrips = activeTrips.Where(t => 
            t.Status == TripStatus.Open && 
            t.DepartureAt > DateTime.UtcNow).ToList();

        if (!futureOpenTrips.Any()) return;

        foreach (var trip in futureOpenTrips)
        {
            trip.Status = TripStatus.Cancelled;
            _uow.Trips.Update(trip);
            
            // Notificar a pasajeros confirmados
            if (trip.ConfirmedPassengerUids != null)
            {
                foreach (var passengerUid in trip.ConfirmedPassengerUids)
            {
                await _notifications.SendNotificationAsync(
                    passengerUid, 
                    "Viaje Cancelado", 
                    $"El viaje con {trip.DriverName} ha sido cancelado automáticamente por el sistema.", 
                    Domain.Enums.NotificationType.TripCancelled, 
                    trip.Id, 
                    trip.DriverUid, 
                    trip.DriverName);
                }
            }
            
            // También deberíamos cancelar/rechazar solicitudes pendientes si las hubiera
            var pendingRequests = await _uow.TripRequests.GetByTripIdAsync(trip.Id);
            foreach (var req in pendingRequests.Where(r => r.Status == RequestStatus.Pending))
            {
                req.Status = RequestStatus.Rejected;
                _uow.TripRequests.Update(req);
                
                await _notifications.SendNotificationAsync(
                    req.PassengerUid,
                    "Solicitud rechazada",
                    $"El viaje de {trip.DriverName} ha sido cancelado.",
                    NotificationType.TripRejected,
                    trip.Id,
                    trip.DriverUid,
                    trip.DriverName);
            }
        }

        await _uow.SaveChangesAsync();
    }

    // ──── Rutas admin ────

    public async Task<List<TripRouteDto>> GetAllRoutesAsync()
    {
        var routes = await _uow.TripRoutes.GetAllAsync();
        return routes.Select(r => new TripRouteDto { Id = r.Id, Name = r.Name }).ToList();
    }

    public async Task<TripRouteDto> CreateRouteAsync(string name)
    {
        var route = new TripRoute { Id = Guid.NewGuid(), Name = name.Trim() };
        await _uow.TripRoutes.AddAsync(route);
        await _uow.SaveChangesAsync();
        return new TripRouteDto { Id = route.Id, Name = route.Name };
    }

    public async Task<TripRouteDto> UpdateRouteAsync(Guid id, string name)
    {
        var route = await _uow.TripRoutes.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Route {id} not found.");
        route.Name = name.Trim();
        _uow.TripRoutes.Update(route);
        await _uow.SaveChangesAsync();
        return new TripRouteDto { Id = route.Id, Name = route.Name };
    }

    public async Task DeleteRouteAsync(Guid id)
    {
        var route = await _uow.TripRoutes.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Route {id} not found.");
        _uow.TripRoutes.Delete(route);
        await _uow.SaveChangesAsync();
    }

    // ──── Reglas admin ────

    public async Task<List<TripRuleDto>> GetAllRulesAsync()
    {
        var rules = await _uow.TripRules.GetAllAsync();
        return rules.Select(r => new TripRuleDto { Id = r.Id, Text = r.Text }).ToList();
    }

    public async Task<TripRuleDto> CreateRuleAsync(string text)
    {
        var rule = new TripRule { Id = Guid.NewGuid(), Text = text.Trim() };
        await _uow.TripRules.AddAsync(rule);
        await _uow.SaveChangesAsync();
        return new TripRuleDto { Id = rule.Id, Text = rule.Text };
    }

    public async Task<TripRuleDto> UpdateRuleAsync(Guid id, string text)
    {
        var rule = await _uow.TripRules.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Rule {id} not found.");
        rule.Text = text.Trim();
        _uow.TripRules.Update(rule);
        await _uow.SaveChangesAsync();
        return new TripRuleDto { Id = rule.Id, Text = rule.Text };
    }

    public async Task DeleteRuleAsync(Guid id)
    {
        var rule = await _uow.TripRules.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Rule {id} not found.");
        _uow.TripRules.Delete(rule);
        await _uow.SaveChangesAsync();
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
        RuleTexts = trip.RuleTexts,
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
