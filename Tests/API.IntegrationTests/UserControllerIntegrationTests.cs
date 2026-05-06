using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.DTOs.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace API.IntegrationTests
{
    public class UserControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public UserControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        // Requisito: RF1 | Protección RNF-001 (Control de Acceso)
        [Fact]
        public async Task Put_UpdateProfile_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            var updateDto = new UpdateProfileDto { DisplayName = "Hacker" };

            // Act
            var response = await _client.PutAsJsonAsync("/api/users/me", updateDto);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
