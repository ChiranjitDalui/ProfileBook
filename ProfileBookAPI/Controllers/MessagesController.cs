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

        // SEND Message by receiver id
        [HttpPost("{receiverId}")]
        public async Task<IActionResult> SendMessage(int receiverId, [FromBody] MessageDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.MessageContent))
                return BadRequest("Message content is required.");

            var senderIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(senderIdClaim, out var senderId))
                return Unauthorized();

            if (senderId == receiverId)
                return BadRequest("You cannot message yourself.");

            var receiverExists = await _context.Users.AnyAsync(u => u.Id == receiverId);
            if (!receiverExists) return NotFound("Receiver not found.");

            var message = new Message
            {
                MessageContent = dto.MessageContent,
                SenderId = senderId,
                ReceiverId = receiverId,
                TimeStamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Return created resource (you can modify to return DTO)
            return CreatedAtAction(nameof(GetMessageById), new { id = message.Id }, new { message.Id, message.MessageContent, message.TimeStamp });
        }

        // Helper to fetch single message (for CreatedAtAction)
        [HttpGet("message/{id}")]
        public async Task<IActionResult> GetMessageById(int id)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var msg = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.Id == id)
                .Select(m => new {
                    m.Id,
                    m.MessageContent,
                    m.TimeStamp,
                    Sender = m.Sender.Username,
                    Receiver = m.Receiver.Username
                })
                .FirstOrDefaultAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            if (msg == null) return NotFound();

            return Ok(msg);
        }

        // GET Messages between logged-in user and another user (by id)
        [HttpGet("withUser/{otherUserId}")]
        public async Task<IActionResult> GetMessages(int otherUserId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var messages = await _context.Messages
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
                    Receiver = m.Receiver.Username,
                    m.SenderId,
                    m.ReceiverId
                })
                .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return Ok(messages);
        }

        // SEND message by username
        [HttpPost("to/{username}")]
        public async Task<IActionResult> SendMessageByUsername(string username, [FromBody] MessageDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.MessageContent))
                return BadRequest("Message content is required.");

            var senderIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(senderIdClaim, out var senderId)) return Unauthorized();

            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (receiver == null) return NotFound("Receiver not found.");

            if (receiver.Id == senderId) return BadRequest("You cannot message yourself.");

            var message = new Message
            {
                MessageContent = dto.MessageContent,
                SenderId = senderId,
                ReceiverId = receiver.Id,
                TimeStamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMessageById), new { id = message.Id }, new { message.Id, message.MessageContent });
        }

        // GET conversation by username
        [HttpGet("with/username/{username}")]
        public async Task<IActionResult> GetMessagesByUsername(string username)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var otherUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (otherUser == null) return NotFound("User not found.");

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var messages = await _context.Messages
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
                    Receiver = m.Receiver.Username,
                    m.SenderId,
                    m.ReceiverId
                })
                .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return Ok(messages);
        }
    }
}
