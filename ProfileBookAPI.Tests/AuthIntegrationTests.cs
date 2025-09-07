using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace ProfileBookAPI.Tests
{
    public class AuthIntegrationTests : IClassFixture<IntegrationTestFactory>
    {
        private readonly IntegrationTestFactory _factory;
        private readonly System.Net.Http.HttpClient _client;

        public AuthIntegrationTests(IntegrationTestFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Register_NewUser_ReturnsOk()
        {
            var payload = new { Username = "test_register_user", PasswordHash = "password123" };
            var res = await _client.PostAsJsonAsync("/api/auth/register", payload);
            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var text = await res.Content.ReadAsStringAsync();
            text.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var payload = new { Username = "doesnotexist", PasswordHash = "wrong" };
            var res = await _client.PostAsJsonAsync("/api/auth/login", payload);
            res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
