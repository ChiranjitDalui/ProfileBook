using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProfileBookAPI.Data;
using ProfileBookAPI.Models;

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
        public IActionResult UpdateUser(int id, [FromBody] User updatedUser)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound("User not found.");

            user.Username = updatedUser.Username;
            if (!string.IsNullOrEmpty(updatedUser.PasswordHash))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updatedUser.PasswordHash);

            user.Role = updatedUser.Role;

            _context.SaveChanges();
            return Ok("User updated successfully.");
        }

        // DELETE User (Admin only)
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound("User not found.");

            // Delete Profile
            var profile = _context.Profiles.FirstOrDefault(p => p.UserId == id);
            if (profile != null)
                _context.Profiles.Remove(profile);

            // Delete Messages (sender or receiver)
            var messages = _context.Messages.Where(m => m.SenderId == id || m.ReceiverId == id).ToList();
            _context.Messages.RemoveRange(messages);

            // Delete Reports (reporting or reported)
            var reports = _context.Reports.Where(r => r.ReportingUserId == id || r.ReportedUserId == id).ToList();
            _context.Reports.RemoveRange(reports);

            // Delete Group Memberships
            var groupMembers = _context.GroupMembers.Where(gm => gm.UserId == id).ToList();
            _context.GroupMembers.RemoveRange(groupMembers);

            // Delete Comments
            var comments = _context.Comments.Where(c => c.UserId == id).ToList();
            _context.Comments.RemoveRange(comments);

            // Finally, delete User
            _context.Users.Remove(user);

            _context.SaveChanges();

            return Ok("User and related data deleted successfully.");
        }

    }
}
