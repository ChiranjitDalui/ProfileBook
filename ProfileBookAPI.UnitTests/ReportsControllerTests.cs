using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProfileBookAPI.Controllers;
using ProfileBookAPI.Data;
using ProfileBookAPI.Models;
using Xunit;
using Moq;

namespace ProfileBookAPI.UnitTests
{
    public class ReportsControllerTests
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
        public void ReportUser_AsUser_CreatesReport()
        {
            var ctx = CreateContext(nameof(ReportUser_AsUser_CreatesReport));
            var reportedId = 777;
            ctx.Users.Add(new User { Id = reportedId, Username = "victim", PasswordHash = "x" });
            ctx.SaveChanges();

            var ctrl = new ReportsController(ctx);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUser(1) } // reporter id =1
            };

            var dto = new { Reason = "Test report reason" };
            var res = ctrl.ReportUser(reportedId, new ReportDto { Reason = "Test report reason" }) as OkObjectResult;
            res.Should().NotBeNull();
            ctx.Reports.Should().Contain(r => r.ReportedUserId == reportedId && r.Reason == "Test report reason");
        }

        [Fact]
        public void GetReports_AsAdmin_ReturnsReportsList()
        {
            var ctx = CreateContext(nameof(GetReports_AsAdmin_ReturnsReportsList));
            // seed a report and users
            ctx.Users.Add(new User { Id = 2, Username = "reporter", PasswordHash = "x" });
            ctx.Users.Add(new User { Id = 3, Username = "victim", PasswordHash = "x" });
            ctx.SaveChanges();
            ctx.Reports.Add(new Report { ReportedUserId = 3, ReportingUserId = 2, Reason = "r" });
            ctx.SaveChanges();

            var ctrl = new ReportsController(ctx);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUser(99, "Admin") }
            };

            var res = ctrl.GetReports() as OkObjectResult;
            res.Should().NotBeNull();
            var list = res!.Value;
            list.Should().NotBeNull();
        }
    }
}
