using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs.Users;
using Application.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace API.IntegrationTests;

// 1. Heredamos de TU fábrica personalizada
public class UserControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Mock<IUserService> _userServiceMock = new();

    public UserControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Put_UpdateProfile_WithValidToken_ReturnsOk()
    {
        // Arrange
        var testUid = TestAuthHandler.DefaultUid; // "test_uid_123"
        var updateDto = new UpdateProfileDto { DisplayName = "Brayan Pilla" };
        var expectedResponse = new UserProfileDto { FirebaseUid = testUid, DisplayName = "Brayan Pilla" };

        // Configuramos el Mock de la capa de aplicación
        _userServiceMock
            .Setup(s => s.UpdateProfileAsync(testUid, It.IsAny<UpdateProfileDto>()))
            .ReturnsAsync(expectedResponse);

        // 2. Usamos WithWebHostBuilder SOLO para meter el Mock específico de esta prueba.
        // Todo lo demás (BD en memoria, Auth falso) ya lo hace tu fábrica por detrás.
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Inyectamos el Mock en el contenedor para que el controlador lo use
                services.AddScoped(_ => _userServiceMock.Object);
            });
        }).CreateClient();

        // Agregamos la cabecera de autenticación usando tu esquema "Test"
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);

        // Act
        var response = await client.PutAsJsonAsync("/api/users/me", updateDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        
        Assert.NotNull(result);
        Assert.Equal("Brayan Pilla", result.DisplayName);
        
        // Verificación con Moq
        _userServiceMock.Verify(s => s.UpdateProfileAsync(testUid, It.IsAny<UpdateProfileDto>()), Times.Once);
    }
}