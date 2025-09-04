using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProfileBookAPI.Data;
using ProfileBookAPI.Models;
using System.Security.Claims;

namespace ProfileBookAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // USER: Report another user
        [HttpPost("{reportedUserId}")]
        public IActionResult ReportUser(int reportedUserId, [FromBody] ReportDto dto)
        {
            var reportingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (reportingUserId == reportedUserId)
                return BadRequest(new { message = "You cannot report yourself." });

            if (!_context.Users.Any(u => u.Id == reportedUserId))
                return NotFound(new { message = "Reported user not found." });

            var report = new Report
            {
                Reason = dto.Reason,
                ReportingUserId = reportingUserId,
                ReportedUserId = reportedUserId
            };

            _context.Reports.Add(report);
            _context.SaveChanges();

            return Ok(new { message = "User reported successfully." });
        }

        // ADMIN: View all reports
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetReports()
        {
            var reports = _context.Reports
                .Include(r => r.ReportingUser)
                .Include(r => r.ReportedUser)
                .ToList();

            var result = reports.Select(r => new
            {
                r.Id,
                r.Reason,
                r.TimeStamp,
                ReportingUser = r.ReportingUser?.Username,
                ReportedUser = r.ReportedUser?.Username
            });

            return Ok(result);
        }
    }
}
