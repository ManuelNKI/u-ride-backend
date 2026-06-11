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

    // ═══════════════════ TRIPS ═══════════════════

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
    /// Obtiene los viajes activos (pending o in_progress) del conductor autenticado.
    /// </summary>
    [HttpGet("active")]
    [Authorize]
    public async Task<ActionResult<List<TripDto>>> GetActiveTrips()
    {
        var uid = GetFirebaseUid();
        var trips = await _tripService.GetActiveTripsAsync(uid);
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

    /// <summary>
    /// Actualiza un viaje existente (solo el conductor, solo si está open).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<TripDto>> UpdateTrip(
        Guid id, [FromBody] UpdateTripDto dto)
    {
        var uid = GetFirebaseUid();
        var trip = await _tripService.UpdateTripAsync(id, uid, dto);
        return Ok(trip);
    }

    /// <summary>
    /// Elimina un viaje (solo el conductor, solo si está open).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteTrip(Guid id)
    {
        var uid = GetFirebaseUid();
        await _tripService.DeleteTripAsync(id, uid);
        return NoContent();
    }

    // ═══════════════════ TRACKING ═══════════════════

    /// <summary>
    /// Actualiza la ubicación en tiempo real del conductor.
    /// </summary>
    [HttpPost("{id:guid}/live-location")]
    [Authorize]
    public IActionResult SetLiveLocation(Guid id, [FromBody] DriverLocationDto location)
    {
        var uid = GetFirebaseUid();
        if (location.DriverUid != uid)
            return Forbid();

        _tripService.SetDriverLiveLocation(id, location);
        return Ok();
    }

    /// <summary>
    /// Obtiene la ubicación en tiempo real del conductor.
    /// </summary>
    [HttpGet("{id:guid}/live-location")]
    [Authorize]
    public ActionResult<DriverLocationDto> GetLiveLocation(Guid id)
    {
        var loc = _tripService.GetDriverLiveLocation(id);
        if (loc is null)
            return NoContent();

        return Ok(loc);
    }

    // ═══════════════════ ROUTES (Admin) ═══════════════════

    /// <summary>
    /// Lista todas las rutas predefinidas.
    /// </summary>
    [HttpGet("routes")]
    public async Task<ActionResult<List<TripRouteDto>>> GetRoutes()
    {
        var routes = await _tripService.GetAllRoutesAsync();
        return Ok(routes);
    }

    /// <summary>
    /// Crea una nueva ruta predefinida.
    /// </summary>
    [HttpPost("routes")]
    [Authorize]
    public async Task<ActionResult<TripRouteDto>> CreateRoute([FromBody] NameRequest request)
    {
        var route = await _tripService.CreateRouteAsync(request.Name);
        return Created($"api/trips/routes/{route.Id}", route);
    }

    /// <summary>
    /// Actualiza una ruta predefinida.
    /// </summary>
    [HttpPut("routes/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<TripRouteDto>> UpdateRoute(Guid id, [FromBody] NameRequest request)
    {
        var route = await _tripService.UpdateRouteAsync(id, request.Name);
        return Ok(route);
    }

    /// <summary>
    /// Elimina una ruta predefinida.
    /// </summary>
    [HttpDelete("routes/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteRoute(Guid id)
    {
        await _tripService.DeleteRouteAsync(id);
        return NoContent();
    }

    // ═══════════════════ RULES (Admin) ═══════════════════

    /// <summary>
    /// Lista todas las reglas predefinidas.
    /// </summary>
    [HttpGet("rules")]
    public async Task<ActionResult<List<TripRuleDto>>> GetRules()
    {
        var rules = await _tripService.GetAllRulesAsync();
        return Ok(rules);
    }

    /// <summary>
    /// Crea una nueva regla predefinida.
    /// </summary>
    [HttpPost("rules")]
    [Authorize]
    public async Task<ActionResult<TripRuleDto>> CreateRule([FromBody] TextRequest request)
    {
        var rule = await _tripService.CreateRuleAsync(request.Text);
        return Created($"api/trips/rules/{rule.Id}", rule);
    }

    /// <summary>
    /// Actualiza una regla predefinida.
    /// </summary>
    [HttpPut("rules/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<TripRuleDto>> UpdateRule(Guid id, [FromBody] TextRequest request)
    {
        var rule = await _tripService.UpdateRuleAsync(id, request.Text);
        return Ok(rule);
    }

    /// <summary>
    /// Elimina una regla predefinida.
    /// </summary>
    [HttpDelete("rules/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        await _tripService.DeleteRuleAsync(id);
        return NoContent();
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

// ──── Request models ────

public class UpdateTripStatusRequest
{
    public string Status { get; set; } = null!;
}

public class NameRequest
{
    public string Name { get; set; } = null!;
}

public class TextRequest
{
    public string Text { get; set; } = null!;
}
