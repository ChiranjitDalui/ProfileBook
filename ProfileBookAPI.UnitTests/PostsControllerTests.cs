using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
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
using System.Text;

namespace ProfileBookAPI.UnitTests
{
    public class PostsControllerTests
    {
        private AppDbContext CreateContext(string dbName)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(opts);
        }

        private ClaimsPrincipal GetUserPrincipal(int userId, string role = "User")
        {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        [Fact]
        public void CreatePost_WithoutImage_ReturnsPost()
        {
            var ctx = CreateContext(nameof(CreatePost_WithoutImage_ReturnsPost));
            var mockEnv = new Mock<IWebHostEnvironment>();
            // use a temp dir for webroot
            var temp = Path.Combine(Path.GetTempPath(), "profilebook_test_wwwroot");
            if (!Directory.Exists(temp)) Directory.CreateDirectory(temp);
            mockEnv.Setup(e => e.WebRootPath).Returns(temp);

            var ctrl = new PostsController(ctx, mockEnv.Object);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(1) }
            };

            var dto = new PostCreateDto { Content = "hello", Image = null };
            var res = ctrl.CreatePost(dto) as OkObjectResult;
            res.Should().NotBeNull();
            ctx.Posts.Should().Contain(p => p.Content == "hello" && p.Status == "Pending");
        }

        [Fact]
        public void ApprovePost_AsAdmin_ChangesStatusAndCreatesNotification()
        {
            var ctx = CreateContext(nameof(ApprovePost_AsAdmin_ChangesStatusAndCreatesNotification));
            var p = new Post { Content = "to approve", Status = "Pending", UserId = 10 };
            ctx.Posts.Add(p);
            ctx.SaveChanges();

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            var ctrl = new PostsController(ctx, mockEnv.Object);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(2, "Admin") }
            };

            var result = ctrl.ApprovePost(p.Id) as OkObjectResult;
            result.Should().NotBeNull();
            var saved = ctx.Posts.Find(p.Id);
            saved!.Status.Should().Be("Approved");
            ctx.Notifications.Should().Contain(n => n.UserId == p.UserId);
        }

        [Fact]
        public void CreatePost_WithImage_SavesFileAndPath()
        {
            var ctx = CreateContext(nameof(CreatePost_WithImage_SavesFileAndPath));
            var tempRoot = Path.Combine(Path.GetTempPath(), "profilebook_test_wwwroot2");
            if (!Directory.Exists(tempRoot)) Directory.CreateDirectory(tempRoot);

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(tempRoot);

            var ctrl = new PostsController(ctx, mockEnv.Object);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(5) }
            };

            // mock IFormFile
            var content = "fake image content";
            var bytes = Encoding.UTF8.GetBytes(content);
            var ms = new MemoryStream(bytes);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("pic.png");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyTo(It.IsAny<Stream>())).Callback<Stream>(s => ms.CopyTo(s));

            var dto = new PostCreateDto { Content = "img post", Image = fileMock.Object };
            var res = ctrl.CreatePost(dto) as OkObjectResult;
            res.Should().NotBeNull();
            ctx.Posts.Should().Contain(p => p.Content == "img post" && p.PostImage != null);
        }
    }
}
