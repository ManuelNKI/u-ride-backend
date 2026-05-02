using System.Security.Claims;
using Application.DTOs.Common;
using Application.DTOs.Trips;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
    }

    /// <summary>
    /// Publica un nuevo viaje. El DriverUid y DriverName se extraen del token.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TripDto>> CreateTrip([FromBody] CreateTripDto dto)
    {
        var uid = GetFirebaseUid();
        var name = GetDisplayName();

        var trip = await _tripService.CreateTripAsync(uid, name, dto);
        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
    }

    /// <summary>
    /// Obtiene un viaje por su ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripDto>> GetById(Guid id)
    {
        var trip = await _tripService.GetByIdAsync(id);
        if (trip is null)
            return NotFound();

        return Ok(trip);
    }

    /// <summary>
    /// Busca viajes abiertos con asientos disponibles.
    /// Soporta filtros por zona y fecha de salida.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<PagedResultDto<TripDto>>> Search(
        [FromQuery] TripSearchDto search)
    {
        var result = await _tripService.SearchTripsAsync(search);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene los viajes publicados por el conductor autenticado.
    /// </summary>
    [HttpGet("my-trips")]
    [Authorize]
    public async Task<ActionResult<List<TripDto>>> GetMyTrips()
    {
        var uid = GetFirebaseUid();
        var trips = await _tripService.GetDriverTripsAsync(uid);
        return Ok(trips);
    }

    /// <summary>
    /// Actualiza el estado de un viaje (solo el conductor).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize]
    public async Task<ActionResult<TripDto>> UpdateStatus(
        Guid id, [FromBody] UpdateTripStatusRequest request)
    {
        var uid = GetFirebaseUid();
        var trip = await _tripService.UpdateStatusAsync(id, uid, request.Status);
        return Ok(trip);
    }

    // ──── Helpers ────
    private string GetFirebaseUid()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue("user_id")
           ?? throw new UnauthorizedAccessException("Firebase UID not found in token.");

    private string GetDisplayName()
        => User.FindFirstValue("name")
           ?? User.FindFirstValue(ClaimTypes.Name)
           ?? "Unknown";
}

public class UpdateTripStatusRequest
{
    public string Status { get; set; } = null!;
}
