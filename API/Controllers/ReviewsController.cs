using System.Security.Claims;
using Application.DTOs.Reviews;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Crea una calificación post-viaje.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create([FromBody] CreateReviewDto dto)
    {
        var uid = GetFirebaseUid();
        var review = await _reviewService.CreateReviewAsync(uid, dto);
        return CreatedAtAction(nameof(GetByTrip), new { tripId = review.TripId }, review);
    }

    /// <summary>
    /// Obtiene las calificaciones de un viaje.
    /// </summary>
    [HttpGet("trip/{tripId:guid}")]
    public async Task<ActionResult<List<ReviewDto>>> GetByTrip(Guid tripId)
    {
        var reviews = await _reviewService.GetByTripAsync(tripId);
        return Ok(reviews);
    }

    /// <summary>
    /// Obtiene las calificaciones recibidas por un usuario.
    /// </summary>
    [HttpGet("user/{uid}")]
    public async Task<ActionResult<List<ReviewDto>>> GetByUser(string uid)
    {
        var reviews = await _reviewService.GetReceivedByUserAsync(uid);
        return Ok(reviews);
    }

    private string GetFirebaseUid()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue("user_id")
           ?? throw new UnauthorizedAccessException("Firebase UID not found in token.");
}
