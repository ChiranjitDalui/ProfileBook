using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using ProfileBookAPI.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ProfileBookAPI.Tests
{
    public class ProfilesIntegrationTests : IClassFixture<IntegrationTestFactory>
    {
        private readonly IntegrationTestFactory _factory;
        private readonly System.Net.Http.HttpClient _client;

        public ProfilesIntegrationTests(IntegrationTestFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetMyProfile_ReturnsOk()
        {
            // TestAuthHandler creates user id = 1 by default; ensure a profile for user 1 exists via seeding
            using (var scope = _factory.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (await ctx.Profiles.FindAsync(1) == null)
                {
                    ctx.Users.Add(new ProfileBookAPI.Models.User { Id = 1, Username = "testuser", PasswordHash = "x" });
                    ctx.Profiles.Add(new ProfileBookAPI.Models.Profile { Id = 1, UserId = 1, FullName = "Seeded User" });
                    await ctx.SaveChangesAsync();
                }
            }

            var res = await _client.GetAsync("/api/profiles/me");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await res.Content.ReadAsStringAsync();
            json.Should().Contain("Seeded User");
        }

        [Fact]
        public async Task UpdateProfile_ReturnsOkAndPersists()
        {
            // Make sure profile exists for user 1
            using (var scope = _factory.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (await ctx.Profiles.FindAsync(1) == null)
                {
                    ctx.Users.Add(new ProfileBookAPI.Models.User { Id = 1, Username = "testuser2", PasswordHash = "x" });
                    ctx.Profiles.Add(new ProfileBookAPI.Models.Profile { Id = 1, UserId = 1, FullName = "Before" });
                    await ctx.SaveChangesAsync();
                }
            }

            var updateDto = new { FullName = "Updated Name", Bio = "Integration test bio" };

            // <-- CORRECT ROUTE: PUT /api/profiles/me (not /api/profiles)
            var putRes = await _client.PutAsJsonAsync("/api/profiles/me", updateDto);
            putRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var getRes = await _client.GetAsync("/api/profiles/me");
            getRes.EnsureSuccessStatusCode();
            var content = await getRes.Content.ReadAsStringAsync();
            content.Should().Contain("Updated Name");
            content.Should().Contain("Integration test bio");
        }

    }
}
