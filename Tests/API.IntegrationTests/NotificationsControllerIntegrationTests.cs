using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests;

public class NotificationsControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public NotificationsControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCount_AndPatchMarksAsRead()
    {
        // Arrange
        var uid = $"uid_{Guid.NewGuid():N}";
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Uid", uid);
        var (user, notification) = await SeedUserAndNotificationAsync(uid, NotificationType.TripCompleted);

        // Act 1: unread-count
        var unread1 = await client.GetFromJsonAsync<UnreadCountResponse>("/api/notifications/unread-count");

        // Assert 1
        Assert.NotNull(unread1);
        Assert.Equal(1, unread1!.Count);

        // Act 2: list notifications
        var list = await client.GetFromJsonAsync<List<NotificationResponse>>("/api/notifications");

        // Assert 2
        Assert.NotNull(list);
        Assert.Single(list!);
        Assert.Equal(notification.Id, Guid.Parse(list![0].Id));
        Assert.Equal("trip_completed", list[0].Type);
        Assert.False(list[0].Read);

        // Act 3: mark read
        var patch = await client.PatchAsync($"/api/notifications/{notification.Id}/read", JsonContent.Create(new { }));

        // Assert 3
        Assert.Equal(HttpStatusCode.NoContent, patch.StatusCode);

        // Act 4: unread-count again
        var unread2 = await client.GetFromJsonAsync<UnreadCountResponse>("/api/notifications/unread-count");

        // Assert 4
        Assert.NotNull(unread2);
        Assert.Equal(0, unread2!.Count);
    }

    [Fact]
    public async Task GetMyNotifications_WithoutAuthHeader_IsStillAuthorized_ByTestScheme()
    {
        // Arrange
        var uid = $"uid_{Guid.NewGuid():N}";
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Uid", uid);
        await SeedUserAndNotificationAsync(uid, NotificationType.System);

        // Act
        var response = await client.GetAsync("/api/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<(User user, Notification notification)> SeedUserAndNotificationAsync(
        string uid,
        NotificationType type)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Database.EnsureCreatedAsync();

        var user = new User
        {
            FirebaseUid = uid,
            Email = $"{uid}@example.test",
            DisplayName = "Test User",
            EmailVerified = true
        };

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserUid = uid,
            User = user,
            Title = "Test",
            Message = "Hello",
            Type = type,
            Read = false
        };

        db.Users.Add(user);
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return (user, notification);
    }

    private sealed class UnreadCountResponse
    {
        public int Count { get; set; }
    }

    private sealed class NotificationResponse
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool Read { get; set; }
        public string CreatedAt { get; set; } = null!;
        public string? TripId { get; set; }
        public string? DriverUid { get; set; }
        public string? DriverName { get; set; }
    }
}
