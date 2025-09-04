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
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MessagesController(AppDbContext context)
        {
            _context = context;
        }

        // SEND Message
        [HttpPost("{receiverId}")]
        public IActionResult SendMessage(int receiverId, [FromBody] string messageContent)
        {
            var senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (senderId == receiverId)
                return BadRequest("You cannot message yourself.");

            if (!_context.Users.Any(u => u.Id == receiverId))
                return NotFound("Receiver not found.");

            var message = new Message
            {
                MessageContent = messageContent,
                SenderId = senderId,
                ReceiverId = receiverId
            };

            _context.Messages.Add(message);
            _context.SaveChanges();

            return Ok("Message sent successfully.");
        }

        // GET Messages between logged-in user and another user
        [HttpGet("{otherUserId}")]
        public IActionResult GetMessages(int otherUserId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var messages = _context.Messages
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                    (m.SenderId == otherUserId && m.ReceiverId == userId))
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderBy(m => m.TimeStamp)
                .Select(m => new
                {
                    m.Id,
                    m.MessageContent,
                    m.TimeStamp,
                    Sender = m.Sender.Username,
                    Receiver = m.Receiver.Username
                })
                .ToList();

            return Ok(messages);
        }


        // SEND message by username
        [HttpPost("to/{username}")]
        public IActionResult SendMessageByUsername(string username, [FromBody] string messageContent)
        {
            var senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var receiver = _context.Users.FirstOrDefault(u => u.Username == username);
            if (receiver == null) return NotFound("Receiver not found.");

            if (receiver.Id == senderId)
                return BadRequest("You cannot message yourself.");

            var message = new Message
            {
                MessageContent = messageContent,
                SenderId = senderId,
                ReceiverId = receiver.Id
            };

            _context.Messages.Add(message);
            _context.SaveChanges();

            return Ok("Message sent successfully.");
        }

        // GET conversation by username
        [HttpGet("with/{username}")]
        public IActionResult GetMessagesByUsername(string username)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var otherUser = _context.Users.FirstOrDefault(u => u.Username == username);
            if (otherUser == null) return NotFound("User not found.");

            var messages = _context.Messages
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == otherUser.Id) ||
                    (m.SenderId == otherUser.Id && m.ReceiverId == userId))
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderBy(m => m.TimeStamp)
                .Select(m => new
                {
                    m.Id,
                    m.MessageContent,
                    m.TimeStamp,
                    Sender = m.Sender.Username,
                    Receiver = m.Receiver.Username
                })
                .ToList();

            return Ok(messages);
        }

    }
}
