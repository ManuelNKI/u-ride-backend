using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.DTOs.TripRequests;
using Application.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace API.IntegrationTests;

public class TripRequestsControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Mock<ITripRequestService> _tripRequestServiceMock = new();

    public TripRequestsControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithUid(string uid)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Reemplazamos el ITripRequestService real por el Mock
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITripRequestService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddScoped(_ => _tripRequestServiceMock.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add("X-Test-Uid", uid);
        return client;
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 1: POST /api/triprequests - Camino Feliz (Crear Solicitud)
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateRequest_ValidDto_ReturnsCreatedWithLocationHeader()
    {
        // Arrange
        var passengerUid = $"passenger_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(passengerUid);

        var tripId = Guid.NewGuid();
        var createDto = new CreateTripRequestDto
        {
            TripId = tripId
        };

        // Sincronizado al 100% con tu TripRequestDto real
        var expectedResult = new TripRequestDto
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            PassengerUid = passengerUid,
            PassengerName = "Unknown", // Proviene de tu GetDisplayName() si el handler de prueba no inyecta "name"
            Status = "Pending",
            PaymentStatus = "Pending",
            DriverRated = false,
            DriverReported = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _tripRequestServiceMock
            .Setup(s => s.CreateRequestAsync(passengerUid, "Unknown", It.IsAny<CreateTripRequestDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var response = await client.PostAsJsonAsync("/api/triprequests", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TripRequestDto>();
        Assert.NotNull(result);
        Assert.Equal(expectedResult.Id, result.Id);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("Pending", result.PaymentStatus);

        // Validar que CreatedAtAction resolvió la cabecera Location hacia /api/triprequests/trip/{guid}
        var locationHeader = response.Headers.Location?.ToString();
        Assert.NotNull(locationHeader);
        Assert.Contains($"/api/TripRequests/trip/{tripId}", locationHeader, StringComparison.OrdinalIgnoreCase);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 2: PATCH /api/triprequests/{id}/accept - Conductor Acepta Solicitud
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Accept_ExistingRequest_ReturnsOkWithUpdatedStatus()
    {
        // Arrange
        var driverUid = $"driver_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(driverUid);
        var requestId = Guid.NewGuid();

        var expectedResult = new TripRequestDto
        {
            Id = requestId,
            TripId = Guid.NewGuid(),
            PassengerUid = "passenger_uid_123",
            PassengerName = "Carlos",
            Status = "Accepted",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _tripRequestServiceMock
            .Setup(s => s.AcceptRequestAsync(requestId, driverUid))
            .ReturnsAsync(expectedResult);

        // Act - Construimos un HttpRequestMessage manual para hacer el PATCH sin cuerpo (null) de forma limpia
        using var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"/api/triprequests/{requestId}/accept");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        requestMessage.Headers.Add("X-Test-Uid", driverUid);

        var response = await client.SendAsync(requestMessage);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TripRequestDto>();
        Assert.NotNull(result);
        Assert.Equal("Accepted", result.Status);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 3: PATCH /api/triprequests/{id}/cancel - Pasajero Cancela Solicitud
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Cancel_ExistingRequest_ReturnsOkWithCancelledStatus()
    {
        // Arrange
        var passengerUid = $"passenger_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(passengerUid);
        var requestId = Guid.NewGuid();

        var expectedResult = new TripRequestDto
        {
            Id = requestId,
            TripId = Guid.NewGuid(),
            PassengerUid = passengerUid,
            PassengerName = "Brayan",
            Status = "Cancelled",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _tripRequestServiceMock
            .Setup(s => s.CancelRequestAsync(requestId, passengerUid))
            .ReturnsAsync(expectedResult);

        // Act
        using var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"/api/triprequests/{requestId}/cancel");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        requestMessage.Headers.Add("X-Test-Uid", passengerUid);

        var response = await client.SendAsync(requestMessage);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TripRequestDto>();
        Assert.NotNull(result);
        Assert.Equal("Cancelled", result.Status);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 4: GET /api/triprequests/my-requests - Obtener solicitudes del pasajero
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetMyRequests_AuthenticatedUser_ReturnsOkWithList()
    {
        // Arrange
        var passengerUid = $"passenger_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(passengerUid);

        var expectedList = new List<TripRequestDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TripId = Guid.NewGuid(),
                PassengerUid = passengerUid,
                PassengerName = "Brayan",
                Status = "Pending",
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _tripRequestServiceMock
            .Setup(s => s.GetByPassengerAsync(passengerUid))
            .ReturnsAsync(expectedList);

        // Act
        var response = await client.GetAsync("/api/triprequests/my-requests");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<TripRequestDto>>();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(passengerUid, result[0].PassengerUid);
    }
}