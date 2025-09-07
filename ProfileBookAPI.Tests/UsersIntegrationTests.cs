using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using ProfileBookAPI.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ProfileBookAPI.Tests
{
    public class UsersIntegrationTests : IClassFixture<IntegrationTestFactory>
    {
        private readonly IntegrationTestFactory _factory;
        private readonly System.Net.Http.HttpClient _client;

        public UsersIntegrationTests(IntegrationTestFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetAllUsers_AsAdmin_ReturnsList()
        {
            // Seed a user so list is not empty
            using (var scope = _factory.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (await ctx.Users.FindAsync(99) == null)
                {
                    ctx.Users.Add(new ProfileBookAPI.Models.User { Id = 99, Username = "user99", PasswordHash = "x" });
                    await ctx.SaveChangesAsync();
                }
            }

            var res = await _client.GetAsync("/api/users");
            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var s = await res.Content.ReadAsStringAsync();
            s.Should().Contain("user99");
        }

        [Fact]
        public async Task DeleteUser_AsAdmin_RemovesUser()
        {
            int seedId = 1001;
            using (var scope = _factory.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (await ctx.Users.FindAsync(seedId) == null)
                {
                    ctx.Users.Add(new ProfileBookAPI.Models.User { Id = seedId, Username = "delete_me", PasswordHash = "x" });
                    await ctx.SaveChangesAsync();
                }
            }

            var delRes = await _client.DeleteAsync($"/api/users/{seedId}");
            delRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // verify from DB
            using (var scope = _factory.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await ctx.Users.FindAsync(seedId);
                user.Should().BeNull();
            }
        }
    }
}
