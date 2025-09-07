using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProfileBookAPI.Controllers;
using ProfileBookAPI.Data;
using ProfileBookAPI.Models;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;

namespace ProfileBookAPI.UnitTests
{
    public class ProfilesControllerTests
    {
        private AppDbContext CreateContext(string dbName)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(opts);
        }

        private ClaimsPrincipal GetUser(int id, string role = "User")
        {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        [Fact]
        public async Task UpdateMyProfile_WhenExists_UpdatesFields()
        {
            var ctx = CreateContext(nameof(UpdateMyProfile_WhenExists_UpdatesFields));
            var user = new User { Id = 90, Username = "u90", PasswordHash = "x" };
            ctx.Users.Add(user);
            ctx.Profiles.Add(new Profile { UserId = 90, FullName = "old", Bio = "oldbio" });
            await ctx.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns("/tmp");

            var ctrl = new ProfilesController(ctx, mockEnv.Object);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUser(90) }
            };

            var updated = new Profile { FullName = "newname", Email = "e@x.com", Phone = "123", Bio = "newbio" };
            var res = ctrl.UpdateMyProfile(updated) as OkObjectResult;
            res.Should().NotBeNull();

            var p = await ctx.Profiles.FirstOrDefaultAsync(pr => pr.UserId == 90);
            p!.FullName.Should().Be("newname");
            p.Bio.Should().Be("newbio");
        }

        [Fact]
        public void GetMyProfile_WhenMissing_ReturnsNotFound()
        {
            var ctx = CreateContext(nameof(GetMyProfile_WhenMissing_ReturnsNotFound));
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns("/tmp");

            var ctrl = new ProfilesController(ctx, mockEnv.Object);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUser(12) }
            };

            var res = ctrl.GetMyProfile();
            res.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
