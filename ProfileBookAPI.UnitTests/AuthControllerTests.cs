using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProfileBookAPI.Controllers;
using ProfileBookAPI.Data;
using ProfileBookAPI.Models;
using Xunit;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace ProfileBookAPI.UnitTests
{
    public class AuthControllerTests
    {
        private AppDbContext CreateContext(string dbName)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(opts);
        }

        [Fact]
        public void Register_NewUser_CreatesUserAndProfile()
        {
            var ctx = CreateContext(nameof(Register_NewUser_CreatesUserAndProfile));
            var mockConfig = new Mock<IConfiguration>();
            var ctrl = new AuthController(ctx, mockConfig.Object);

            var dto = new User { Username = "newuser", PasswordHash = "plain", Role = "User" };

            var res = ctrl.Register(dto) as OkObjectResult;

            res.Should().NotBeNull();

            // Assert user persisted
            ctx.Users.Any(u => u.Username == "newuser").Should().BeTrue();

            // get the saved user id synchronously (InMemory provider supports sync)
            var savedUser = ctx.Users.FirstOrDefault(u => u.Username == "newuser");
            savedUser.Should().NotBeNull();

            ctx.Profiles.Any(p => p.UserId == savedUser!.Id).Should().BeTrue();
        }

        [Fact]
        public void Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var ctx = CreateContext(nameof(Login_InvalidCredentials_ReturnsUnauthorized));
            // seed a user with a password hash for "rightpass"
            var seeded = new User { Username = "u1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("rightpass") };
            ctx.Users.Add(seeded);
            ctx.SaveChanges();

            var mockConfig = new Mock<IConfiguration>();
            var ctrl = new AuthController(ctx, mockConfig.Object);

            var loginDto = new User { Username = "u1", PasswordHash = "wrongpass" };
            var res = ctrl.Login(loginDto);

            res.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void Login_ValidCredentials_ReturnsOkAndToken()
        {
            var ctx = CreateContext(nameof(Login_ValidCredentials_ReturnsOkAndToken));
            var seeded = new User { Username = "u2", PasswordHash = BCrypt.Net.BCrypt.HashPassword("mypwd"), Role = "User" };
            ctx.Users.Add(seeded);
            ctx.SaveChanges();

            var mockConfig = new Mock<IConfiguration>();
            var ctrl = new AuthController(ctx, mockConfig.Object);

            var loginDto = new User { Username = "u2", PasswordHash = "mypwd" };
            var res = ctrl.Login(loginDto) as OkObjectResult;
            res.Should().NotBeNull();
            res!.Value.Should().NotBeNull();
        }
    }
}
