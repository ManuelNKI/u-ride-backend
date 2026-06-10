using Application.DTOs.Appeals;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace API.IntegrationTests;

public class AppealsControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AppealsControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithUid(string uid)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add("X-Test-Uid", uid);
        return client;
    }

    // ═════════════════════════════════════════════════════════════════════
    // ESCENARIO 1: Crear Apelación - Camino Feliz (Usuario Suspendido)
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateAppeal_UserIsSuspended_ReturnsOkAndSavesToDb()
    {
        // Arrange
        var uid = $"suspended_user_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(uid);

        // Sembramos un usuario que SÍ está suspendido a futuro
        await SeedUserAsync(uid, isAdmin: false, suspendedUntil: DateTime.UtcNow.AddDays(5));
        var request = new CreateAppealDto { Reason = "Fue un malentendido con el pasajero." };

        // Act
        var response = await client.PostAsJsonAsync("/api/appeals", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Validar directo en la BD en memoria que se guardó como Pending
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var savedAppeal = await db.Appeals.FirstOrDefaultAsync(a => a.UserId == uid);

        Assert.NotNull(savedAppeal);
        Assert.Equal(AppealStatus.Pending, savedAppeal.Status);
        Assert.Equal("Fue un malentendido con el pasajero.", savedAppeal.Reason);
    }

    // ═════════════════════════════════════════════════════════════════════
    // ESCENARIO 2: Crear Apelación - Rechazo (Usuario No Suspendido)
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateAppeal_UserIsNotSuspended_ReturnsBadRequest()
    {
        // Arrange
        var uid = $"active_user_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(uid);

        // Sembramos un usuario común con SuspendedUntil en null
        await SeedUserAsync(uid, isAdmin: false, suspendedUntil: null);
        var request = new CreateAppealDto { Reason = "No sé por qué apelo si no estoy bloqueado." };

        // Act
        var response = await client.PostAsJsonAsync("/api/appeals", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════
    // ESCENARIO 3: Seguridad - Listar todas las apelaciones sin ser Admin
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetAllAppeals_UserIsNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var uid = $"mortal_user_{Guid.NewGuid():N}";
        using var client = CreateClientWithUid(uid);
        await SeedUserAsync(uid, isAdmin: false); // Usuario común

        // Act
        var response = await client.GetAsync("/api/appeals");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════
    // ESCENARIO 4: Flujo Admin - Procesar y Aprobar Apelación (Quita bloqueo y notifica)
    // ═════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task ProcessAppeal_AdminApproves_LiftsSuspensionAndSendsNotification()
    {
        // Arrange
        var adminUid = $"admin_{Guid.NewGuid():N}";
        var userUid = $"user_{Guid.NewGuid():N}";

        using var client = CreateClientWithUid(adminUid);

        // Sembramos las condiciones: Un admin, un usuario bloqueado y su apelación asociada
        await SeedUserAsync(adminUid, isAdmin: true);
        var user = await SeedUserAsync(userUid, isAdmin: false, suspendedUntil: DateTime.UtcNow.AddDays(10));
        var appealId = await SeedAppealAsync(userUid, "Por favor desbloquéenme.");

        var processDto = new ProcessAppealDto { Approve = true, AdminNotes = "Disculpas aceptadas." };

        // Act
        var response = await client.PostAsJsonAsync($"/api/appeals/{appealId}/process", processDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verificaciones profundas de estado en la BD
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var updatedUser = await db.Users.FindAsync(userUid);
        var updatedAppeal = await db.Appeals.FindAsync(appealId);
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.UserUid == userUid);

        // 1. La suspensión debió quedar en null
        Assert.Null(updatedUser!.SuspendedUntil);

        // 2. La apelación cambió a Approved con las notas del admin
        Assert.Equal(AppealStatus.Approved, updatedAppeal!.Status);
        Assert.Equal("Disculpas aceptadas.", updatedAppeal.AdminNotes);

        // 3. Se generó de forma automática la notificación del sistema
        Assert.NotNull(notification);
        Assert.Equal("Apelación Aprobada", notification.Title);
    }

    // ═════════════════════════════════════════════════════════════════════
    // HELPERS PARA SEMBRAR DATOS (SEEDERS)
    // ═════════════════════════════════════════════════════════════════════
    private async Task<User> SeedUserAsync(string uid, bool isAdmin, DateTime? suspendedUntil = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            FirebaseUid = uid,
            Email = $"{uid}@u-ride.test",
            DisplayName = isAdmin ? "System Admin" : "Standard Driver",
            IsAdmin = isAdmin,
            SuspendedUntil = suspendedUntil,
            EmailVerified = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<int> SeedAppealAsync(string uid, string reason)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var appeal = new Appeal
        {
            UserId = uid,
            Reason = reason,
            Status = AppealStatus.Pending
        };

        db.Appeals.Add(appeal);
        await db.SaveChangesAsync();
        return appeal.Id; // Retornamos el entero autogenerado de la BD
    }
}