using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProfileBookAPI.Data;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

namespace ProfileBookAPI.Tests
{
    // Test auth handler used by tests
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string TestScheme = "TestScheme";
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, TestScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, TestScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    // Test IWebHostEnvironment implementation with file providers
    public class TestWebHostEnvironment : IWebHostEnvironment
    {
        // make properties nullable to avoid non-nullable warnings in the test project
        public string? ApplicationName { get; set; }
        public IFileProvider? ContentRootFileProvider { get; set; }
        public string? ContentRootPath { get; set; }
        public string? EnvironmentName { get; set; }
        public IFileProvider? WebRootFileProvider { get; set; }
        public string? WebRootPath { get; set; }
    }

    // Public factory used by tests
    public class IntegrationTestFactory : WebApplicationFactory<Program>
    {
        private readonly string _inMemoryDbName = Guid.NewGuid().ToString();
        public string TestWebRoot { get; } = Path.Combine(Path.GetTempPath(), "profilebook_test_wwwroot", Guid.NewGuid().ToString());

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // ConfigureTestServices is provided by Microsoft.AspNetCore.TestHost
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext registrations to avoid two providers
                var descriptorsToRemove = services.Where(d =>
                        d.ServiceType == typeof(AppDbContext) ||
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        (d.ImplementationType != null && d.ImplementationType == typeof(AppDbContext)) ||
                        (d.ServiceType != null && d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("DbContextOptions"))
                    ).ToList();

                foreach (var d in descriptorsToRemove)
                {
                    services.Remove(d);
                }

                // Add InMemory DB
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_inMemoryDbName);
                });

                // Replace authentication with Test scheme AND make it the default scheme
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.TestScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.TestScheme;
                    options.DefaultScheme = TestAuthHandler.TestScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.TestScheme, options => { });

                // Ensure webroot exists and set file providers
                Directory.CreateDirectory(TestWebRoot);
                var contentRoot = Directory.GetCurrentDirectory();

                services.AddSingleton<IWebHostEnvironment>(sp =>
                {
                    return new TestWebHostEnvironment
                    {
                        EnvironmentName = Environments.Development,
                        ApplicationName = AppDomain.CurrentDomain.FriendlyName,
                        ContentRootPath = contentRoot,
                        ContentRootFileProvider = new PhysicalFileProvider(contentRoot),
                        WebRootPath = TestWebRoot,
                        WebRootFileProvider = new PhysicalFileProvider(TestWebRoot)
                    };
                });
            });

            builder.UseEnvironment("Development");
        }

        // Optional helper to make intent explicit in tests (returns a client that uses the default TestScheme identity)
        public HttpClient CreateAdminClient()
        {
            // CreateClient will use the default auth scheme we've registered above.
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            return client;
        }
    }
}
