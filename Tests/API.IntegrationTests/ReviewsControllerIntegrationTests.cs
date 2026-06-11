using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.DTOs.Reviews;
using Application.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace API.IntegrationTests;

public class ReviewsControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Mock<IReviewService> _reviewServiceMock = new();

    public ReviewsControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithUid(string uid)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IReviewService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddScoped(_ => _reviewServiceMock.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add("X-Test-Uid", uid);
        return client;
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 1: POST /api/reviews - Camino Feliz (Crear Calificación)
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Create_ValidReview_ReturnsCreatedWithCorrectLocationHeader()
    {
        // Arrange
        var reviewerUid = $"passenger_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(reviewerUid);

        var tripId = Guid.NewGuid();
        var targetUid = "driver_uid_abc";

        // Sincronizado al 100% con tu CreateReviewDto real
        var createDto = new CreateReviewDto
        {
            TripId = tripId,
            ToUid = targetUid,
            Stars = 5,
            Comment = "Excelente viaje, el conductor fue muy amable."
        };

        // Sincronizado al 100% con tu ReviewDto real
        var expectedResult = new ReviewDto
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            FromUid = reviewerUid,
            ToUid = targetUid,
            Stars = createDto.Stars,
            Comment = createDto.Comment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _reviewServiceMock
            .Setup(s => s.CreateReviewAsync(reviewerUid, It.IsAny<CreateReviewDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var response = await client.PostAsJsonAsync("/api/reviews", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ReviewDto>();
        Assert.NotNull(result);
        Assert.Equal(expectedResult.Id, result.Id);
        Assert.Equal(reviewerUid, result.FromUid);
        Assert.Equal(targetUid, result.ToUid);
        Assert.Equal(5, result.Stars);

        // Validar que CreatedAtAction resolvió bien la cabecera Location hacia /api/reviews/trip/{guid}
        var locationHeader = response.Headers.Location?.ToString();
        Assert.NotNull(locationHeader);
        Assert.Contains($"/api/Reviews/trip/{tripId}", locationHeader, StringComparison.OrdinalIgnoreCase);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 2: GET /api/reviews/trip/{tripId} - Obtener por Viaje
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetByTrip_ExistingTrip_ReturnsOkWithListOfReviews()
    {
        // Arrange
        using var client = CreateClientWithUid("any_user_uid");
        var tripId = Guid.NewGuid();

        var expectedReviews = new List<ReviewDto>
        {
            new() { Id = Guid.NewGuid(), TripId = tripId, FromUid = "user_a", ToUid = "user_b", Stars = 4, Comment = "Buen viaje" },
            new() { Id = Guid.NewGuid(), TripId = tripId, FromUid = "user_c", ToUid = "user_b", Stars = 5, Comment = "Todo perfecto" }
        };

        _reviewServiceMock
            .Setup(s => s.GetByTripAsync(tripId))
            .ReturnsAsync(expectedReviews);

        // Act
        var response = await client.GetAsync($"/api/reviews/trip/{tripId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<ReviewDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(tripId, result[0].TripId);
        Assert.Equal(4, result[0].Stars);
    }

    // ═════════════════════════════════════════════════════════════════════
    // TEST 3: GET /api/reviews/user/{uid} - Obtener recibidas por usuario
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetByUser_ExistingUser_ReturnsOkWithReviews()
    {
        // Arrange
        using var client = CreateClientWithUid("any_user_uid");
        var targetUserUid = "target_driver_123";

        var expectedReviews = new List<ReviewDto>
        {
            new() { Id = Guid.NewGuid(), TripId = Guid.NewGuid(), FromUid = "passenger_xyz", ToUid = targetUserUid, Stars = 5, Comment = "Maneja muy bien" }
        };

        _reviewServiceMock
            .Setup(s => s.GetReceivedByUserAsync(targetUserUid))
            .ReturnsAsync(expectedReviews);

        // Act
        var response = await client.GetAsync($"/api/reviews/user/{targetUserUid}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<ReviewDto>>();

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Maneja muy bien", result[0].Comment);
        Assert.Equal(targetUserUid, result[0].ToUid);
    }
}