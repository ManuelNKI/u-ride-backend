using Application.DTOs.TripRequests;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Services;

public class TripRequestService : ITripRequestService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;

    public TripRequestService(IUnitOfWork uow, INotificationService notifications)
    {
        _uow = uow;
        _notifications = notifications;
    }

    public async Task<TripRequestDto> CreateRequestAsync(
        string passengerUid, string passengerName, CreateTripRequestDto dto)
    {
        var trip = await _uow.Trips.GetByIdAsync(dto.TripId)
            ?? throw new KeyNotFoundException($"Trip {dto.TripId} not found.");

        if (trip.Status != TripStatus.Open)
            throw new InvalidOperationException("Trip is not open for requests.");

        if (trip.SeatsAvailable <= 0)
            throw new InvalidOperationException("No seats available.");

        if (trip.DriverUid == passengerUid)
            throw new InvalidOperationException("Driver cannot request their own trip.");

        var request = new TripRequest
        {
            Id = Guid.NewGuid(),
            TripId = dto.TripId,
            PassengerUid = passengerUid,
            PassengerName = passengerName,
            Status = RequestStatus.Pending,
            PaymentStatus = PaymentStatus.Pending
        };

        await _uow.TripRequests.AddAsync(request);
        await _uow.SaveChangesAsync();

        // Notificar al conductor: nueva solicitud recibida.
        // Nota: enviamos tripId para que en frontend (rol conductor) navegue a /app/requests/:tripId.
        await TrySendAsync(
            trip.DriverUid,
            title: "Nueva solicitud de viaje",
            message: $"{passengerName} solicitó unirse a tu viaje ({trip.OriginZone} → {trip.DestinationZone}).",
            type: NotificationType.System,
            tripId: trip.Id);

        // (Opcional) Notificar al pasajero que su solicitud fue enviada.
        // Mantiene coherencia para cuando el usuario está en rol pasajero.
        await TrySendAsync(
            passengerUid,
            title: "Solicitud enviada",
            message: $"Tu solicitud para el viaje de {trip.DriverName} fue enviada.",
            type: NotificationType.System,
            tripId: trip.Id,
            driverUid: trip.DriverUid,
            driverName: trip.DriverName);

        return MapToDto(request);
    }

    /// <summary>
    /// El conductor acepta una solicitud → reduce SeatsAvailable
    /// y agrega el pasajero a ConfirmedPassengerUids de forma transaccional.
    /// </summary>
    public async Task<TripRequestDto> AcceptRequestAsync(Guid requestId, string driverUid)
    {
        var request = await _uow.TripRequests.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException($"Request {requestId} not found.");

        if (request.Trip.DriverUid != driverUid)
            throw new UnauthorizedAccessException("Only the driver can accept requests.");

        if (request.Status != RequestStatus.Pending)
            throw new InvalidOperationException($"Request is already {request.Status}.");

        if (request.Trip.SeatsAvailable <= 0)
            throw new InvalidOperationException("No seats available.");

        // ── Lógica transaccional ──
        request.Status = RequestStatus.Accepted;
        request.Trip.SeatsAvailable--;
        request.Trip.ConfirmedPassengerUids.Add(request.PassengerUid);

        _uow.TripRequests.Update(request);
        _uow.Trips.Update(request.Trip);

        // Incrementar conteo de viajes del pasajero
        var passenger = await _uow.Users.GetByUidAsync(request.PassengerUid);
        if (passenger is not null)
        {
            passenger.PassengerTripsCount++;
            passenger.TripsCount++;
            _uow.Users.Update(passenger);
        }

        // Si no quedan asientos, cerrar el viaje automáticamente
        if (request.Trip.SeatsAvailable == 0)
            request.Trip.Status = TripStatus.Closed;

        await _uow.SaveChangesAsync();

        // Notificar al pasajero: solicitud aceptada.
        await TrySendAsync(
            request.PassengerUid,
            title: "Solicitud aceptada",
            message: $"{request.Trip.DriverName} aceptó tu solicitud. Ya puedes coordinar tu viaje.",
            type: NotificationType.TripAccepted,
            tripId: request.Trip.Id,
            driverUid: request.Trip.DriverUid,
            driverName: request.Trip.DriverName);

        return MapToDto(request);
    }

    public async Task<TripRequestDto> RejectRequestAsync(Guid requestId, string driverUid)
    {
        var request = await _uow.TripRequests.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException($"Request {requestId} not found.");

        if (request.Trip.DriverUid != driverUid)
            throw new UnauthorizedAccessException("Only the driver can reject requests.");

        if (request.Status != RequestStatus.Pending)
            throw new InvalidOperationException($"Request is already {request.Status}.");

        request.Status = RequestStatus.Rejected;
        _uow.TripRequests.Update(request);
        await _uow.SaveChangesAsync();

        // Notificar al pasajero: solicitud rechazada.
        await TrySendAsync(
            request.PassengerUid,
            title: "Solicitud rechazada",
            message: $"{request.Trip.DriverName} rechazó tu solicitud.",
            type: NotificationType.TripRejected,
            tripId: request.Trip.Id,
            driverUid: request.Trip.DriverUid,
            driverName: request.Trip.DriverName);

        return MapToDto(request);
    }

    public async Task<TripRequestDto> CancelRequestAsync(Guid requestId, string passengerUid)
    {
        var request = await _uow.TripRequests.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException($"Request {requestId} not found.");

        if (request.PassengerUid != passengerUid)
            throw new UnauthorizedAccessException("Only the passenger can cancel their request.");

        if (request.Status == RequestStatus.Cancelled)
            throw new InvalidOperationException("Request is already cancelled.");

        // Si estaba aceptada, devolver el asiento
        if (request.Status == RequestStatus.Accepted)
        {
            request.Trip.SeatsAvailable++;
            request.Trip.ConfirmedPassengerUids.Remove(request.PassengerUid);

            if (request.Trip.Status == TripStatus.Closed)
                request.Trip.Status = TripStatus.Open;

            _uow.Trips.Update(request.Trip);
        }

        request.Status = RequestStatus.Cancelled;
        _uow.TripRequests.Update(request);
        await _uow.SaveChangesAsync();

        // Notificar al conductor: el pasajero canceló su solicitud.
        await TrySendAsync(
            request.Trip.DriverUid,
            title: "Solicitud cancelada",
            message: $"{request.PassengerName} canceló su solicitud para tu viaje.",
            type: NotificationType.System,
            tripId: request.Trip.Id);

        return MapToDto(request);
    }

    private async Task TrySendAsync(
        string userUid,
        string title,
        string message,
        NotificationType type,
        Guid? tripId = null,
        string? driverUid = null,
        string? driverName = null)
    {
        await _notifications.SendNotificationAsync(
            userUid,
            title,
            message,
            type,
            tripId,
            driverUid,
            driverName);
    }

    public async Task<List<TripRequestDto>> GetByTripIdAsync(Guid tripId)
    {
        var requests = await _uow.TripRequests.GetByTripIdAsync(tripId);
        return requests.Select(MapToDto).ToList();
    }

    public async Task<List<TripRequestDto>> GetByPassengerAsync(string passengerUid)
    {
        var requests = await _uow.TripRequests.GetByPassengerUidAsync(passengerUid);
        return requests.Select(MapToDto).ToList();
    }

    // ──── Mapping helper ────
    private static TripRequestDto MapToDto(TripRequest r) => new()
    {
        Id = r.Id,
        TripId = r.TripId,
        PassengerUid = r.PassengerUid,
        PassengerName = r.PassengerName,
        Status = r.Status.ToString().ToLowerInvariant(),
        PaymentStatus = r.PaymentStatus.ToString().ToLowerInvariant(),
        DriverRated = r.DriverRated,
        DriverReported = r.DriverReported,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
