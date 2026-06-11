using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using static API.Controllers.PayPalPaymentsController;

namespace API.IntegrationTests;

public class PayPalPaymentsControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Mock<IPayPalCheckoutService> _payPalMock = new();
    private readonly Mock<INotificationService> _notificationsMock = new();
    private WebApplicationFactory<Program> _customFactory = null!;

    public PayPalPaymentsControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithUid(string uid)
    {
        _customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var paypalDescriptors = services.Where(d => d.ServiceType == typeof(IPayPalCheckoutService)).ToList();
                foreach (var desc in paypalDescriptors) services.Remove(desc);
                services.AddScoped(_ => _payPalMock.Object);

                var notifDescriptors = services.Where(d => d.ServiceType == typeof(INotificationService)).ToList();
                foreach (var desc in notifDescriptors) services.Remove(desc);
                services.AddScoped(_ => _notificationsMock.Object);
            });
        });

        var client = _customFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add("X-Test-Uid", uid);
        return client;
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 1: POST orders - Camino Feliz
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateOrder_ValidAcceptedRequest_ReturnsOkWithPayPalUrls()
    {
        // Arrange
        var passengerUid = $"passenger_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(passengerUid);

        var tripId = Guid.NewGuid();
        var tripRequestId = Guid.NewGuid();

        await SeedTripRequestWithIdsAsync(tripRequestId, tripId, passengerUid, RequestStatus.Accepted, PaymentStatus.Pending, 10.00m);

        var expectedResult = new PayPalCreateOrderResult("PAYPAL-ID-123", "https://paypal.com/checkout?token=123");
        _payPalMock
            .Setup(p => p.CreateOrderAsync(It.IsAny<PayPalCreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var dto = new CreateOrderDto { TripRequestId = tripRequestId };

        // Act
        using (var scope = _customFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var seededReq = await db.TripRequests.FindAsync(tripRequestId);
            if (seededReq == null)
            {
                throw new Exception("DEBUG: Seeded TripRequest not found in database!");
            }
            else if (seededReq.Status != RequestStatus.Accepted)
            {
                throw new Exception($"DEBUG: Seeded TripRequest status is {seededReq.Status} instead of Accepted! Trip ID is {seededReq.TripId}, passenger UID is {seededReq.PassengerUid}");
            }
        }

        var response = await client.PostAsJsonAsync("/api/payments/paypal/orders", dto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();

        Assert.NotNull(result);
        Assert.Equal("PAYPAL-ID-123", result.OrderId);
        Assert.Equal("https://paypal.com/checkout?token=123", result.ApproveUrl);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 2: POST orders - Rechazo por estado incorrecto
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateOrder_RequestNotAccepted_ReturnsBadRequest()
    {
        // Arrange
        var passengerUid = $"passenger_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(passengerUid);

        var tripId = Guid.NewGuid();
        var tripRequestId = Guid.NewGuid();

        await SeedTripRequestWithIdsAsync(tripRequestId, tripId, passengerUid, RequestStatus.Pending, PaymentStatus.Pending, 15.50m);

        var dto = new CreateOrderDto { TripRequestId = tripRequestId };

        // Act
        var response = await client.PostAsJsonAsync("/api/payments/paypal/orders", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 3: GET return - Captura de Fondos Exitosa
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Return_SuccessfulCapture_UpdatesDatabaseAndSendsNotification()
    {
        // Arrange
        var passengerUid = $"passenger_{Guid.NewGuid():N}";
        var driverUid = $"driver_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(passengerUid);

        var tripRequestId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        await SeedTripRequestWithIdsAsync(tripRequestId, tripId, passengerUid, RequestStatus.Accepted, PaymentStatus.Pending, 12.00m, driverUid);

        var orderToken = "PAYPAL-TOKEN-ABC";

        var mockOrderInfo = new PayPalOrderInfo(orderToken, "APPROVED", tripRequestId.ToString(), null);
        _payPalMock
            .Setup(p => p.GetOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockOrderInfo);

        var mockCaptureResult = new PayPalCaptureResult(orderToken, "COMPLETED", "CAP-123", "COMPLETED", tripRequestId.ToString(), null);
        _payPalMock
            .Setup(p => p.CaptureOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCaptureResult);

        // Act
        var response = await client.GetAsync($"/api/payments/paypal/return?tripRequestId={tripRequestId}&token={orderToken}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        // Validar base de datos limpia limpiando los estados cacheados en el tracker del test
        using var scope = _customFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.ChangeTracker.Clear(); // ◄ CLAVE: Rompe la caché de lectura para obligar a consultar el cambio real

        var updatedRequest = await db.TripRequests.FindAsync(tripRequestId);

        Assert.NotNull(updatedRequest);
        Assert.Equal(PaymentStatus.Paid, updatedRequest.PaymentStatus);
    }

    // ═════════════════════════════════════════════════════════════════════
    // SEEDER ÚNICO ROBUSTO Y NORMALIZADO
    // ═════════════════════════════════════════════════════════════════════
    private async Task SeedTripRequestWithIdsAsync(
        Guid requestId, Guid tripId, string passengerUid, RequestStatus status, PaymentStatus paymentStatus, decimal price, string driverUid = "some_driver")
    {
        using var scope = _customFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Limpiamos cambios previos por si acaso
        db.ChangeTracker.Clear();

        if (await db.Users.FindAsync(passengerUid) is null)
        {
            db.Users.Add(new User
            {
                FirebaseUid = passengerUid,
                Email = $"{passengerUid}@u-ride.test",
                DisplayName = "Pasajero de Prueba",
                EmailVerified = true
            });
        }

        if (await db.Users.FindAsync(driverUid) is null)
        {
            db.Users.Add(new User
            {
                FirebaseUid = driverUid,
                Email = $"{driverUid}@u-ride.test",
                DisplayName = "System Driver",
                EmailVerified = true
            });
        }

        var trip = new Trip
        {
            Id = tripId,
            Price = price,
            DriverUid = driverUid,
            DriverName = "System Driver",
            RouteName = "Ambato - Quito",
            PaymentMethod = "PayPal",
            OriginZone = "Ficoa",
            DestinationZone = "La Carolina",
            DepartureAt = DateTime.UtcNow.AddDays(1)
        };

        var request = new TripRequest
        {
            Id = requestId,
            TripId = tripId,
            PassengerUid = passengerUid,
            PassengerName = "Pasajero de Prueba",
            Status = status,
            PaymentStatus = paymentStatus,
            Trip = trip
        };

        db.TripRequests.Add(request);
        await db.SaveChangesAsync();

        // Evitamos que las entidades queden amarradas al hilo actual
        db.ChangeTracker.Clear();
    }
}