using System.Security.Claims;
using Application.DTOs.Appeals;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppealsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AppealsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppeal([FromBody] CreateAppealDto request)
    {
        var uid = GetFirebaseUid();
        var user = await _context.Users.FindAsync(uid);
        
        if (user == null) return NotFound(new { message = "Usuario no encontrado." });

        if (user.SuspendedUntil == null || user.SuspendedUntil <= DateTime.UtcNow)
            return BadRequest(new { message = "No puedes apelar porque no estás bloqueado actualmente." });

        var hasPending = await _context.Appeals.AnyAsync(a => a.UserId == uid && a.Status == AppealStatus.Pending);
        if (hasPending) return BadRequest(new { message = "Ya tienes una apelación en revisión." });

        var appeal = new Appeal
        {
            UserId = uid,
            Reason = request.Reason,
            Status = AppealStatus.Pending
        };

        _context.Appeals.Add(appeal);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Apelación enviada correctamente." });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyAppeals()
    {
        var uid = GetFirebaseUid();
        var appeals = await _context.Appeals
            .Include(a => a.User)
            .Where(a => a.UserId == uid)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AppealResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User.DisplayName,
                UserEmail = a.User.Email,
                Reason = a.Reason,
                Status = a.Status.ToString().ToLower(),
                CreatedAt = a.CreatedAt,
                ProcessedAt = a.ProcessedAt,
                AdminNotes = a.AdminNotes
            })
            .ToListAsync();

        return Ok(appeals);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAppeals()
    {
        var adminUid = GetFirebaseUid();
        var admin = await _context.Users.FindAsync(adminUid);
        if (admin == null || !admin.IsAdmin) return Forbid();

        var appeals = await _context.Appeals
            .Include(a => a.User)
            .OrderBy(a => a.Status)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AppealResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User.DisplayName,
                UserEmail = a.User.Email,
                Reason = a.Reason,
                Status = a.Status.ToString().ToLower(),
                CreatedAt = a.CreatedAt,
                ProcessedAt = a.ProcessedAt,
                AdminNotes = a.AdminNotes
            })
            .ToListAsync();

        return Ok(appeals);
    }

    [HttpPost("{id}/process")]
    public async Task<IActionResult> ProcessAppeal(int id, [FromBody] ProcessAppealDto request)
    {
        var adminUid = GetFirebaseUid();
        var admin = await _context.Users.FindAsync(adminUid);
        if (admin == null || !admin.IsAdmin) return Forbid();

        var appeal = await _context.Appeals.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
        if (appeal == null) return NotFound(new { message = "Apelación no encontrada." });

        if (appeal.Status != AppealStatus.Pending)
            return BadRequest(new { message = "Esta apelación ya fue procesada." });

        appeal.Status = request.Approve ? AppealStatus.Approved : AppealStatus.Rejected;
        appeal.ProcessedAt = DateTime.UtcNow;
        appeal.ProcessedById = adminUid;
        appeal.AdminNotes = request.AdminNotes;

        if (request.Approve)
        {
            appeal.User.SuspendedUntil = null;
            _context.Notifications.Add(new Notification
            {
                UserUid = appeal.UserId,
                Title = "Apelación Aprobada",
                Message = "Tu cuenta ha sido reactivada. ¡Bienvenido de nuevo!",
                Type = Domain.Enums.NotificationType.System,
                Read = false
            });
        }
        else
        {
            _context.Notifications.Add(new Notification
            {
                UserUid = appeal.UserId,
                Title = "Apelación Rechazada",
                Message = "Tu apelación de suspensión no fue aprobada.",
                Type = Domain.Enums.NotificationType.System,
                Read = false
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Apelación procesada correctamente." });
    }

    private string GetFirebaseUid()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue("user_id")
           ?? throw new UnauthorizedAccessException("Firebase UID not found in token.");
}
