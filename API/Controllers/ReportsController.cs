using System.Security.Claims;
using Application.DTOs.Common;
using Application.DTOs.Reports;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Crea un reporte contra otro usuario.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ReportDto>> Create([FromBody] CreateReportDto dto)
    {
        var uid = GetFirebaseUid();
        var report = await _reportService.CreateReportAsync(uid, dto);
        return Created(string.Empty, report);
    }

    /// <summary>
    /// [Admin] Obtiene todos los reportes paginados.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ReportDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _reportService.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Resuelve un reporte y aplica una acción (none, warned, suspended).
    /// </summary>
    [HttpPatch("{id:guid}/resolve")]
    public async Task<ActionResult<ReportDto>> Resolve(
        Guid id, [FromBody] ResolveReportRequest request)
    {
        var report = await _reportService.ResolveReportAsync(id, request.Action, request.AdminNotes);
        return Ok(report);
    }

    /// <summary>
    /// Sube una imagen de evidencia a Coolify (localmente en wwwroot/uploads).
    /// </summary>
    [HttpPost("upload-evidence")]
    public async Task<IActionResult> UploadEvidence(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest("File is not an image.");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Devolvemos la ruta relativa para accederla públicamente
        var relativeUrl = $"/uploads/reports/{uniqueFileName}";
        return Ok(new { evidenceUrl = relativeUrl });
    }

    private string GetFirebaseUid()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue("user_id")
           ?? throw new UnauthorizedAccessException("Firebase UID not found in token.");
}

public class ResolveReportRequest
{
    public string Action { get; set; } = null!;
    public string? AdminNotes { get; set; }
}
