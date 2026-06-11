using API.Controllers;
using Application.DTOs.Common;
using Application.DTOs.Reports;
using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static API.Controllers.ReportsController;

namespace API.IntegrationTests;

public class ReportsControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Mock<IReportService> _reportServiceMock = new();

    public ReportsControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithUid(string uid)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Reemplazamos el servicio real por el Mock
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IReportService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddScoped(_ => _reportServiceMock.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add("X-Test-Uid", uid);
        return client;
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 1: POST /api/reports - Camino Feliz (Crear reporte exitoso)
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Create_ValidReport_ReturnsCreatedWithReportDto()
    {
        // Arrange
        var senderUid = $"user_reporter_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(senderUid);

        var createDto = new CreateReportDto
        {
            TripId = Guid.NewGuid(),
            ReportedUid = "user_reported_abc",
            Reason = "El conductor manejaba a exceso de velocidad.",
            EvidenceUrl = "/uploads/reports/test.jpg"
        };

        var expectedResult = new ReportDto
        {
            Id = Guid.NewGuid(),
            ReporterUid = senderUid,
            ReportedUid = createDto.ReportedUid,
            Reason = createDto.Reason,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        _reportServiceMock
            .Setup(s => s.CreateReportAsync(senderUid, It.IsAny<CreateReportDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var response = await client.PostAsJsonAsync("/api/reports", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReportDto>();

        Assert.NotNull(result);
        Assert.Equal(expectedResult.Id, result.Id);
        Assert.Equal("pending", result.Status);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 2: POST /api/reports - Conflicto (Controlador atrapa InvalidOperationException)
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Create_DuplicateReport_ReturnsConflict()
    {
        // Arrange
        var senderUid = $"user_reporter_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(senderUid);
        var createDto = new CreateReportDto { TripId = Guid.NewGuid(), ReportedUid = "uid_123", Reason = "Duplicado" };

        _reportServiceMock
            .Setup(s => s.CreateReportAsync(senderUid, It.IsAny<CreateReportDto>()))
            .ThrowsAsync(new InvalidOperationException("Ya has reportado a este usuario para este viaje."));

        // Act
        var response = await client.PostAsJsonAsync("/api/reports", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var errorMessage = await response.Content.ReadAsStringAsync();
        Assert.Equal("Ya has reportado a este usuario para este viaje.", errorMessage);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 3: GET /api/reports/has-reported?tripId={guid} - Ruta Condicional 1
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task HasReported_WithoutReportedUid_CallsHasReportedForTripAsync()
    {
        // Arrange
        var uid = "test_user_uid";
        using var client = CreateClientWithUid(uid);
        var tripId = Guid.NewGuid();

        _reportServiceMock
            .Setup(s => s.HasReportedForTripAsync(uid, tripId))
            .ReturnsAsync(true);

        // Act
        var response = await client.GetAsync($"/api/reports/has-reported?tripId={tripId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<bool>();
        Assert.True(result);
        _reportServiceMock.Verify(s => s.HasReportedForTripAsync(uid, tripId), Times.Once);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 4: PATCH /api/reports/{id}/resolve - Flujo Admin resolver
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Resolve_Report_ReturnsOkWithUpdatedReport()
    {
        // Arrange
        using var client = CreateClientWithUid("admin_uid");
        var reportId = Guid.NewGuid();
        var request = new ResolveReportRequest { Action = "suspended", AdminNotes = "Comportamiento inaceptable." };

        var expectedResult = new ReportDto { Id = reportId, Status = "resolved" };

        _reportServiceMock
            .Setup(s => s.ResolveReportAsync(reportId, request.Action, request.AdminNotes, It.IsAny<int?>()))
            .ReturnsAsync(expectedResult);

        // Act
        var response = await client.PatchAsJsonAsync($"/api/reports/{reportId}/resolve", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReportDto>();
        Assert.NotNull(result);
        Assert.Equal("resolved", result.Status);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 5: POST /api/reports/upload-evidence - Manejo de archivos físicos (IFormFile)
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task UploadEvidence_ValidImageFile_ReturnsOkWithEvidenceUrl()
    {
        // Arrange
        using var client = CreateClientWithUid("any_uid");

        // Creamos un stream falso que simule ser una imagen JPEG real en bytes
        var fileContent = Encoding.UTF8.GetBytes("fake-image-binary-data");
        var byteArrayContent = new ByteArrayContent(fileContent);
        byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

        // Construimos el contenedor Multipart FormData requerido por IFormFile
        using var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(byteArrayContent, "file", "evidencia_choque.jpg");

        // Act
        var response = await client.PostAsync("/api/reports/upload-evidence", multipartContent);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verificamos que la respuesta retorne la propiedad evidenceUrl estructurada
        var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.NotNull(result);
        Assert.Contains("/uploads/reports/", result.EvidenceUrl);

        // Limpieza: Eliminamos el archivo físico temporal creado en wwwroot/uploads por la prueba
        var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports");
        if (Directory.Exists(expectedPath))
        {
            var directoryInfo = new DirectoryInfo(expectedPath);
            foreach (var file in directoryInfo.GetFiles())
            {
                if (file.Name.EndsWith("_evidencia_choque.jpg"))
                {
                    file.Delete();
                }
            }
        }
    }

    // Helper interno para deserializar el resultado anónimo del upload
    private sealed class UploadResponse
    {
        public string EvidenceUrl { get; set; } = null!;
    }
}