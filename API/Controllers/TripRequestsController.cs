using System.Security.Claims;
using Application.DTOs.TripRequests;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TripRequestsController : ControllerBase
{
    private readonly ITripRequestService _requestService;

    public TripRequestsController(ITripRequestService requestService)
    {
        _requestService = requestService;
    }

    /// <summary>
    /// Crea una solicitud para unirse a un viaje.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TripRequestDto>> CreateRequest([FromBody] CreateTripRequestDto dto)
    {
        var uid = GetFirebaseUid();
        var name = GetDisplayName();

        var request = await _requestService.CreateRequestAsync(uid, name, dto);
        return CreatedAtAction(nameof(GetByTrip), new { tripId = request.TripId }, request);
    }

    /// <summary>
    /// El conductor acepta una solicitud (reduce SeatsAvailable transaccionalmente).
    /// </summary>
    [HttpPatch("{id:guid}/accept")]
    public async Task<ActionResult<TripRequestDto>> Accept(Guid id)
    {
        var uid = GetFirebaseUid();
        var result = await _requestService.AcceptRequestAsync(id, uid);
        return Ok(result);
    }

    /// <summary>
    /// El conductor rechaza una solicitud.
    /// </summary>
    [HttpPatch("{id:guid}/reject")]
    public async Task<ActionResult<TripRequestDto>> Reject(Guid id)
    {
        var uid = GetFirebaseUid();
        var result = await _requestService.RejectRequestAsync(id, uid);
        return Ok(result);
    }

    /// <summary>
    /// El pasajero cancela su propia solicitud.
    /// </summary>
    [HttpPatch("{id:guid}/cancel")]
    public async Task<ActionResult<TripRequestDto>> Cancel(Guid id)
    {
        var uid = GetFirebaseUid();
        var result = await _requestService.CancelRequestAsync(id, uid);
        return Ok(result);
    }

    /// <summary>
    /// El pasajero paga su solicitud aceptada.
    /// </summary>
    [HttpPatch("{id:guid}/pay")]
    public async Task<ActionResult<TripRequestDto>> Pay(Guid id)
    {
        var uid = GetFirebaseUid();
        var result = await _requestService.PayRequestAsync(id, uid);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene todas las solicitudes de un viaje (para el conductor).
    /// </summary>
    [HttpGet("trip/{tripId:guid}")]
    public async Task<ActionResult<List<TripRequestDto>>> GetByTrip(Guid tripId)
    {
        var requests = await _requestService.GetByTripIdAsync(tripId);
        return Ok(requests);
    }

    /// <summary>
    /// Obtiene las solicitudes del pasajero autenticado.
    /// </summary>
    [HttpGet("my-requests")]
    public async Task<ActionResult<List<TripRequestDto>>> GetMyRequests()
    {
        var uid = GetFirebaseUid();
        var requests = await _requestService.GetByPassengerAsync(uid);
        return Ok(requests);
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
