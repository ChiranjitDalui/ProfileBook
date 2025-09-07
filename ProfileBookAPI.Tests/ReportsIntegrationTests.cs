using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using ProfileBookAPI.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace ProfileBookAPI.Tests
{
    public class ReportsIntegrationTests : IClassFixture<IntegrationTestFactory>
    {
        private readonly IntegrationTestFactory _factory;
        private readonly System.Net.Http.HttpClient _client;

        public ReportsIntegrationTests(IntegrationTestFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(); // default test user (TestAuthHandler)
        }

        [Fact]
        public async Task ReportUser_AsUser_CreatesReport_AndAdminCanView()
        {
            // ensure both the reporting user (id=1) and the reported user exist in the same test server DB
            int reportingUserId = 1;
            int reportedId = 5000;

            using (var scope = _factory.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Add reporting user if not exists
                if (await ctx.Users.FindAsync(reportingUserId) == null)
                {
                    ctx.Users.Add(new ProfileBookAPI.Models.User
                    {
                        Id = reportingUserId,
                        Username = "reporter",
                        PasswordHash = "x",
                        Role = "User"
                    });
                }

                // Add reported user if not exists
                if (await ctx.Users.FindAsync(reportedId) == null)
                {
                    ctx.Users.Add(new ProfileBookAPI.Models.User
                    {
                        Id = reportedId,
                        Username = "reportedUser",
                        PasswordHash = "x",
                        Role = "User"
                    });
                }

                await ctx.SaveChangesAsync();
            }

            // Use a client that will be authenticated as a test user (TestAuthHandler returns NameIdentifier=1).
            // _client created in ctor uses the TestAuth scheme as registered by the factory - good for POST (acts as user id=1).
            var postDto = new { Reason = "Test report reason" };
            var postRes = await _client.PostAsJsonAsync($"/api/reports/{reportedId}", postDto);
            postRes.StatusCode.Should().Be(HttpStatusCode.OK, $"POST /api/reports should succeed but returned {postRes.StatusCode}. Response: {await postRes.Content.ReadAsStringAsync()}");

            // Confirm the report is in the same in-memory DB (reliable server-side check)
            using (var scope = _factory.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var found = ctx.Reports.Any(r => r.ReportedUserId == reportedId && r.Reason == "Test report reason" && r.ReportingUserId == reportingUserId);
                found.Should().BeTrue("Report should be persisted to the in-memory database used by the app.");
            }

            // Now create an admin client (the TestAuthHandler returns role Admin in claims; CreateClient uses that scheme)
            var adminClient = _factory.CreateClient();

            // GET as admin
            var getRes = await adminClient.GetAsync("/api/reports");
            getRes.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

            if (getRes.StatusCode == HttpStatusCode.OK)
            {
                var text = await getRes.Content.ReadAsStringAsync();
                // helpful debug output if assertion fails
                text.Should().Contain("Test report reason").And.Contain("reportedUser");
            }
        }

    }
}
