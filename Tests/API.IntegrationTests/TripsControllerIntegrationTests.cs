using API.Controllers;
using Application.DTOs.Common;
using Application.DTOs.Trips;
using Application.Services; // Importa tu ITripService, DriverLocationDto, TripRouteDto y TripRuleDto reales
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace API.IntegrationTests;

public class TripsControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Mock<ITripService> _tripServiceMock = new();

    public TripsControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithUid(string uid)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITripService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddScoped(_ => _tripServiceMock.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add("X-Test-Uid", uid);
        return client;
    }

    // ═════════════════════════════════════════════════════════════════════
    // SECCIÓN 1: PRUEBAS DE VIAJES (TRIPS)
    // ═════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateTrip_ValidDto_ReturnsCreatedWithLocationHeader()
    {
        // Arrange
        var driverUid = $"driver_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(driverUid);
        var tripId = Guid.NewGuid();

        var createDto = new CreateTripDto
        {
            RouteName = "Ambato - Quito",
            PaymentMethod = "Paypal",
            OriginZone = "Ficoa",
            DestinationZone = "La Carolina",
            DepartureAt = DateTime.UtcNow.AddHours(2),
            SeatsTotal = 4,
            Price = 5.50m,
            Vehicle = new VehicleInfoDto { Plate = "TBA-1234", Model = "Yaris", Brand = "Toyota", Color = "Negro" },
            Rules = new TripRulesDto { Punctuality = true, Respect = true, NoSensitiveData = false }
        };

        var expectedResult = new TripDto
        {
            Id = tripId,
            DriverUid = driverUid,
            DriverName = "Unknown",
            RouteName = createDto.RouteName,
            PaymentMethod = createDto.PaymentMethod,
            OriginZone = createDto.OriginZone,
            DestinationZone = createDto.DestinationZone,
            SeatsTotal = createDto.SeatsTotal,
            SeatsAvailable = createDto.SeatsTotal,
            Price = createDto.Price,
            Status = "Open",
            Vehicle = createDto.Vehicle,
            Rules = createDto.Rules,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _tripServiceMock
            .Setup(s => s.CreateTripAsync(driverUid, "Unknown", It.IsAny<CreateTripDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var response = await client.PostAsJsonAsync("/api/trips", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TripDto>();
        Assert.NotNull(result);
        Assert.Equal(tripId, result.Id);
        Assert.Equal("Open", result.Status);

        var locationHeader = response.Headers.Location?.ToString();
        Assert.NotNull(locationHeader);
        Assert.Contains($"/api/Trips/{tripId}", locationHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetById_TripDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        using var client = CreateClientWithUid("any_uid");
        var targetId = Guid.NewGuid();

        _tripServiceMock
            .Setup(s => s.GetByIdAsync(targetId))
            .ReturnsAsync((TripDto?)null);

        // Act
        var response = await client.GetAsync($"/api/trips/{targetId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTrip_AuthenticatedDriver_ReturnsNoContent()
    {
        // Arrange
        var driverUid = "driver_owner_123";
        using var client = CreateClientWithUid(driverUid);
        var tripId = Guid.NewGuid();

        _tripServiceMock
            .Setup(s => s.DeleteTripAsync(tripId, driverUid))
            .Returns(Task.CompletedTask);

        // Act
        var response = await client.DeleteAsync($"/api/trips/{tripId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _tripServiceMock.Verify(s => s.DeleteTripAsync(tripId, driverUid), Times.Once);
    }

    // ═════════════════════════════════════════════════════════════════════
    // SECCIÓN 2: PRUEBAS DE SEGUIMIENTO (TRACKING)
    // ═════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SetLiveLocation_UidMismatch_ReturnsForbidden()
    {
        // Arrange
        var realAuthenticatedUid = "driver_real_777";
        using var client = CreateClientWithUid(realAuthenticatedUid);
        var tripId = Guid.NewGuid();

        // Corregido: Mapeo exacto usando tus propiedades reales 'Lat' y 'Lng'
        var locationDto = new DriverLocationDto
        {
            DriverUid = "hacker_uid_999",
            Lat = -1.249,
            Lng = -78.625,
            Active = true,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/trips/{tripId}/live-location", locationDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _tripServiceMock.Verify(s => s.SetDriverLiveLocation(It.IsAny<Guid>(), It.IsAny<DriverLocationDto>()), Times.Never);
    }

    [Fact]
    public async Task SetLiveLocation_CorrectUid_ReturnsOk()
    {
        // Arrange
        var driverUid = "driver_legit_888";
        using var client = CreateClientWithUid(driverUid);
        var tripId = Guid.NewGuid();

        // Corregido: Mapeo exacto con 'Lat' y 'Lng'
        var locationDto = new DriverLocationDto
        {
            DriverUid = driverUid,
            Lat = -1.25,
            Lng = -78.62,
            Active = true,
            UpdatedAt = DateTime.UtcNow
        };

        _tripServiceMock.Setup(s => s.SetDriverLiveLocation(tripId, It.IsAny<DriverLocationDto>()));

        // Act
        var response = await client.PostAsJsonAsync($"/api/trips/{tripId}/live-location", locationDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════
    // SECCIÓN 3: PRUEBAS DE MANTENIMIENTO ADMIN (ROUTES Y RULES)
    // ═════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateRoute_ValidRequest_ReturnsCreated()
    {
        // Arrange
        using var client = CreateClientWithUid("admin_uid");
        var nameRequest = new NameRequest { Name = "Ruta Periférica Norte" };
        var expectedRoute = new TripRouteDto { Id = Guid.NewGuid(), Name = nameRequest.Name };

        _tripServiceMock
            .Setup(s => s.CreateRouteAsync(nameRequest.Name))
            .ReturnsAsync(expectedRoute);

        // Act
        var response = await client.PostAsJsonAsync("/api/trips/routes", nameRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TripRouteDto>();
        Assert.NotNull(result);
        Assert.Equal(nameRequest.Name, result!.Name);
    }

    [Fact]
    public async Task CreateRule_ValidRequest_ReturnsCreated()
    {
        // Arrange
        using var client = CreateClientWithUid("admin_uid");
        var textRequest = new TextRequest { Text = "Prohibido fumar en el vehículo." };
        var expectedRule = new TripRuleDto { Id = Guid.NewGuid(), Text = textRequest.Text };

        _tripServiceMock
            .Setup(s => s.CreateRuleAsync(textRequest.Text))
            .ReturnsAsync(expectedRule);

        // Act
        var response = await client.PostAsJsonAsync("/api/trips/rules", textRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TripRuleDto>();
        Assert.NotNull(result);
        Assert.Equal(textRequest.Text, result!.Text);
    }
}