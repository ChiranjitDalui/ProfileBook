using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProfileBookAPI.Data;
using ProfileBookAPI.Models;

namespace ProfileBookAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Only admins can manage groups
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GroupsController(AppDbContext context)
        {
            _context = context;
        }

        // CREATE Group
        [HttpPost]
        public IActionResult CreateGroup([FromBody] string groupName)
        {
            var group = new Group { GroupName = groupName };
            _context.Groups.Add(group);
            _context.SaveChanges();
            return Ok($"Group '{groupName}' created successfully.");
        }

        // ADD User to Group
        [HttpPost("{groupId}/add/{userId}")]
        public IActionResult AddUserToGroup(int groupId, int userId)
        {
            if (!_context.Groups.Any(g => g.Id == groupId))
                return NotFound("Group not found.");

            if (!_context.Users.Any(u => u.Id == userId))
                return NotFound("User not found.");

            if (_context.GroupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == userId))
                return BadRequest("User is already in this group.");

            var member = new GroupMember { GroupId = groupId, UserId = userId };
            _context.GroupMembers.Add(member);
            _context.SaveChanges();

            return Ok("User added to group.");
        }

        // REMOVE User from Group
        [HttpDelete("{groupId}/remove/{userId}")]
        public IActionResult RemoveUserFromGroup(int groupId, int userId)
        {
            var member = _context.GroupMembers.FirstOrDefault(gm => gm.GroupId == groupId && gm.UserId == userId);
            if (member == null) return NotFound("User not found in group.");

            _context.GroupMembers.Remove(member);
            _context.SaveChanges();

            return Ok("User removed from group.");
        }

        // VIEW All Groups
        [HttpGet]
        public IActionResult GetGroups()
        {
            var groups = _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .ToList();

            var result = groups.Select(g => new
            {
                g.Id,
                g.GroupName,
                Members = g.Members?.Select(m => m.User.Username).ToList()
            });

            return Ok(result);
        }
    }
}
