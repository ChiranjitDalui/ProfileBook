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
    [Authorize(Roles = "Admin")] // Only admins can manage users
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET All Users
        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Role
                })
                .ToList();

            return Ok(users);
        }

        // GET User by Id
        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound("User not found.");

            return Ok(new { user.Id, user.Username, user.Role });
        }
        // UPDATE User (change role, username)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound("User not found.");

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isSelf = id == currentUserId;

            if (string.IsNullOrWhiteSpace(dto.Username))
                return BadRequest("Username is required.");

            // If trying to change your own role, ensure at least one other Admin remains
            if (isSelf && !string.IsNullOrWhiteSpace(dto.Role) && !dto.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var adminCount = _context.Users.Count(u => u.Role == "Admin");
                if (adminCount <= 1)
                    return BadRequest("You are the last admin. Create another admin before changing your own role.");
            }

            user.Username = dto.Username;
            if (!string.IsNullOrWhiteSpace(dto.Role)) user.Role = dto.Role;

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            _context.SaveChanges();
            return Ok(new { message = "User updated successfully." });
        }

        // DELETE User (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound("User not found.");

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Block self delete
            if (id == currentUserId)
                return BadRequest("You cannot delete your own account.");

            // Prevent deleting the last admin
            if (user.Role == "Admin")
            {
                var adminCount = _context.Users.Count(u => u.Role == "Admin");
                if (adminCount <= 1)
                    return BadRequest("Cannot delete the last admin. Create another admin first.");
            }

            // Cascade removals as you already do
            var profile = _context.Profiles.FirstOrDefault(p => p.UserId == id);
            if (profile != null) _context.Profiles.Remove(profile);

            var messages = _context.Messages.Where(m => m.SenderId == id || m.ReceiverId == id).ToList();
            _context.Messages.RemoveRange(messages);

            var reports = _context.Reports.Where(r => r.ReportingUserId == id || r.ReportedUserId == id).ToList();
            _context.Reports.RemoveRange(reports);

            var groupMembers = _context.GroupMembers.Where(gm => gm.UserId == id).ToList();
            _context.GroupMembers.RemoveRange(groupMembers);

            var comments = _context.Comments.Where(c => c.UserId == id).ToList();
            _context.Comments.RemoveRange(comments);

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok(new { message = "User and related data deleted successfully." });
        }
    }
}
