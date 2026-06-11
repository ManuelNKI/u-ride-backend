using Application.DTOs.Vehicles;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyVehicles()
    {
        var uid = GetUserId();
        var vehicles = await _vehicleService.GetVehiclesByUserAsync(uid);
        return Ok(vehicles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVehicle(int id)
    {
        var uid = GetUserId();
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id, uid);
        return Ok(vehicle);
    }

    [HttpPost]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleDto dto)
    {
        var uid = GetUserId();
        var vehicle = await _vehicleService.CreateVehicleAsync(uid, dto);
        return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleDto dto)
    {
        var uid = GetUserId();
        var vehicle = await _vehicleService.UpdateVehicleAsync(id, uid, dto);
        return Ok(vehicle);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var uid = GetUserId();
        await _vehicleService.DeleteVehicleAsync(id, uid);
        return NoContent();
    }
}
