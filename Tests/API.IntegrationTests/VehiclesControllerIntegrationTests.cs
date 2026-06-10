using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.DTOs.Vehicles; // Importación directa de tus DTOs reales
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace API.IntegrationTests;

public class VehiclesControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Mock<IVehicleService> _vehicleServiceMock = new();

    public VehiclesControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithUid(string uid)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IVehicleService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddScoped(_ => _vehicleServiceMock.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add("X-Test-Uid", uid);
        return client;
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 1: POST /api/vehicles - Camino Feliz (Crear Vehículo)
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateVehicle_ValidDto_ReturnsCreatedWithCorrectLocationHeader()
    {
        // Arrange
        var driverUid = $"driver_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(driverUid);

        // Sincronizado con tus propiedades [Required] y de rango
        var createDto = new CreateVehicleDto
        {
            Brand = "Chevrolet",
            ModelOrBusNumber = "Sail",
            Plate = "PBA-5678",
            Color = "Plomo",
            Seats = 5
        };

        // Sincronizado al 100% con tu VehicleDto real de salida
        var expectedResult = new VehicleDto
        {
            Id = 42,
            Brand = createDto.Brand,
            ModelOrBusNumber = createDto.ModelOrBusNumber,
            Plate = createDto.Plate,
            Color = createDto.Color,
            Seats = createDto.Seats,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _vehicleServiceMock
            .Setup(s => s.CreateVehicleAsync(driverUid, It.IsAny<CreateVehicleDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var response = await client.PostAsJsonAsync("/api/vehicles", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("PBA-5678", result.Plate);
        Assert.Equal("Sail", result.ModelOrBusNumber);
        Assert.Equal(5, result.Seats);

        // Validar que CreatedAtAction armó la URL usando el ID entero: /api/vehicles/42
        var locationHeader = response.Headers.Location?.ToString();
        Assert.NotNull(locationHeader);
        Assert.Contains($"/api/Vehicles/42", locationHeader, StringComparison.OrdinalIgnoreCase);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 2: GET /api/vehicles/me - Listar mis vehículos
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetMyVehicles_AuthenticatedUser_ReturnsOkWithList()
    {
        // Arrange
        var driverUid = $"driver_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(driverUid);

        var expectedList = new List<VehicleDto>
        {
            new()
            {
                Id = 1,
                Brand = "Kia",
                ModelOrBusNumber = "Sportage",
                Plate = "ABC-123",
                Color = "Blanco",
                Seats = 5,
                CreatedAt = DateTime.UtcNow
            }
        };

        _vehicleServiceMock
            .Setup(s => s.GetVehiclesByUserAsync(driverUid))
            .ReturnsAsync(expectedList);

        // Act
        var response = await client.GetAsync("/api/vehicles/me");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<VehicleDto>>();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("ABC-123", result[0].Plate);
        Assert.Equal("Sportage", result[0].ModelOrBusNumber);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 3: PUT /api/vehicles/{id} - Actualizar Vehículo
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task UpdateVehicle_ValidDto_ReturnsOkWithUpdatedVehicle()
    {
        // Arrange
        var driverUid = $"driver_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(driverUid);
        var vehicleId = 99;

        // Como tus campos de actualización son [Required], enviamos el objeto completo modificado
        var updateDto = new UpdateVehicleDto
        {
            Brand = "Hyundai",
            ModelOrBusNumber = "Accent",
            Plate = "TBA-999",
            Color = "Rojo Tunado",
            Seats = 4
        };

        var expectedResult = new VehicleDto
        {
            Id = vehicleId,
            Brand = updateDto.Brand,
            ModelOrBusNumber = updateDto.ModelOrBusNumber,
            Plate = updateDto.Plate,
            Color = updateDto.Color,
            Seats = updateDto.Seats,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _vehicleServiceMock
            .Setup(s => s.UpdateVehicleAsync(vehicleId, driverUid, It.IsAny<UpdateVehicleDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var response = await client.PutAsJsonAsync($"/api/vehicles/{vehicleId}", updateDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(result);
        Assert.Equal("Rojo Tunado", result.Color);
        Assert.Equal(4, result.Seats);
        Assert.NotNull(result.UpdatedAt);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 4: DELETE /api/vehicles/{id} - Eliminar Vehículo
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task DeleteVehicle_ExistingVehicle_ReturnsNoContent()
    {
        // Arrange
        var driverUid = "driver_delete_123";
        using var client = CreateClientWithUid(driverUid);
        var vehicleId = 10;

        _vehicleServiceMock
            .Setup(s => s.DeleteVehicleAsync(vehicleId, driverUid))
            .Returns(Task.CompletedTask);

        // Act
        var response = await client.DeleteAsync($"/api/vehicles/{vehicleId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _vehicleServiceMock.Verify(s => s.DeleteVehicleAsync(vehicleId, driverUid), Times.Once);
    }
}