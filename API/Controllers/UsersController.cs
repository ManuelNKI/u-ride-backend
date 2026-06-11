using System.Security.Claims;
using Application.DTOs.Users;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Sincroniza un usuario de Firebase hacia SQL Server.
    /// Se invoca la primera vez que el usuario entra en la app.
    /// </summary>
    [HttpPost("sync")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> SyncUser([FromBody] SyncUserDto dto)
    {
        // Verificar que el UID del token coincida con el del DTO
        var uid = GetFirebaseUid();
        if (uid != dto.FirebaseUid)
            return Forbid();

        var profile = await _userService.SyncUserAsync(dto);
        return Ok(profile);
    }

    /// <summary>
    /// Obtiene el perfil del usuario autenticado.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile()
    {
        var uid = GetFirebaseUid();
        var profile = await _userService.GetProfileAsync(uid);

        if (profile is null)
            return NotFound(new { message = "User profile not found. Please sync first." });

        return Ok(profile);
    }

    /// <summary>
    /// Obtiene el perfil público de cualquier usuario por su UID.
    /// </summary>
    [HttpGet("{uid}")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile(string uid)
    {
        var profile = await _userService.GetProfileAsync(uid);
        if (profile is null)
            return NotFound();

        return Ok(profile);
    }

    /// <summary>
    /// Actualiza el perfil del usuario autenticado.
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var uid = GetFirebaseUid();
        var profile = await _userService.UpdateProfileAsync(uid, dto);
        return Ok(profile);
    }

    // ═══════════════════ ADMIN ═══════════════════

    /// <summary>
    /// [Admin] Lista todos los usuarios con paginación.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _userService.GetAllUsersAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Actualiza el perfil de cualquier usuario.
    /// </summary>
    [HttpPut("{uid}")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> AdminUpdateProfile(
        string uid, [FromBody] UpdateProfileDto dto)
    {
        var profile = await _userService.AdminUpdateProfileAsync(uid, dto);
        return Ok(profile);
    }

    /// <summary>
    /// [Admin] Activa/desactiva un usuario.
    /// </summary>
    [HttpPost("{uid}/toggle-disabled")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> ToggleDisabled(string uid)
    {
        var profile = await _userService.ToggleDisabledAsync(uid);
        return Ok(profile);
    }

    /// <summary>
    /// [Admin] Suspende a un usuario hasta una fecha dada.
    /// </summary>
    [HttpPost("{uid}/suspend")]
    [Authorize]
    public async Task<IActionResult> SuspendUser(string uid, [FromBody] SuspendRequest request)
    {
        await _userService.SuspendUserAsync(uid, request.Until);
        return Ok(new { message = $"User {uid} suspended until {request.Until:u}" });
    }

    /// <summary>
    /// [Admin] Levanta la suspensión de un usuario.
    /// </summary>
    [HttpPost("{uid}/unsuspend")]
    [Authorize]
    public async Task<IActionResult> UnsuspendUser(string uid)
    {
        await _userService.UnsuspendUserAsync(uid);
        return Ok(new { message = $"User {uid} suspension lifted." });
    }

    // ──── Helper: Extraer FirebaseUid del token JWT ────
    private string GetFirebaseUid()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue("user_id")
           ?? throw new UnauthorizedAccessException("Firebase UID not found in token.");
}

public class SuspendRequest
{
    public DateTime Until { get; set; }
}
