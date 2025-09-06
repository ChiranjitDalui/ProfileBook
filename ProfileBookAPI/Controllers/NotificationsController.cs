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
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // Get my notifications
        [HttpGet("me")]
        public IActionResult GetMyNotifications()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var notifications = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return Ok(notifications);
        }

        // Mark notification as read
        [HttpPut("{id}/read")]
        public IActionResult MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var notif = _context.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId);
            if (notif == null) return NotFound();

            notif.IsRead = true;
            _context.SaveChanges();

            return Ok(new { message = "Marked as read" });
        }
    }
}
