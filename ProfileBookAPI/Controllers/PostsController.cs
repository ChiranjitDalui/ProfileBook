using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileBookAPI.Data;
using ProfileBookAPI.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;


namespace ProfileBookAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PostsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // CREATE Post (User)
        [HttpPost]
        public IActionResult CreatePost([FromForm] PostCreateDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            string? filePath = null;
            if (dto.Image != null)
            {
                string uploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
                filePath = Path.Combine("uploads", fileName);
                using (var stream = new FileStream(Path.Combine(_env.WebRootPath, filePath), FileMode.Create))
                {
                    dto.Image.CopyTo(stream);
                }
            }

            var post = new Post
            {
                Content = dto.Content,
                PostImage = filePath,
                UserId = userId,
                Status = "Pending"
            };

            _context.Posts.Add(post);
            _context.SaveChanges();
            return Ok(post);
        }

        // Temporary endpoint to see all post(Pending, Approved, Rejected) along with their IDs
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAllPosts()
        {
            return Ok(_context.Posts.ToList());
        }

        // GET Approved Posts (Users)
        [HttpGet("approved")]
        [AllowAnonymous]
        public IActionResult GetApprovedPosts()
        {
            var posts = _context.Posts
                .Where(p => p.Status == "Approved")
                .ToList();
            return Ok(posts);
        }

        // APPROVE Post (Admin)
        [HttpPut("approve/{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult ApprovePost(int id)
        {
            var post = _context.Posts.FirstOrDefault(p => p.Id == id);
            if (post == null) return NotFound();

            post.Status = "Approved";
            _context.SaveChanges();
          return Ok(new { message = "Post approved." });
        }

        // REJECT Post (Admin)
        [HttpPut("reject/{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult RejectPost(int id)
        {
            var post = _context.Posts.FirstOrDefault(p => p.Id == id);
            if (post == null) return NotFound();

            post.Status = "Rejected";
            _context.SaveChanges();
           return Ok(new { message = "Post rejected." });
        }

        // LIKE Post (User)
        [HttpPost("{id}/like")]
        public IActionResult LikePost(int id)
        {
            var post = _context.Posts.FirstOrDefault(p => p.Id == id && p.Status == "Approved");
            if (post == null) return NotFound("Post not found or not approved yet.");

            post.Likes++;
            _context.SaveChanges();
            return Ok(new { message = "Post liked successfully", likes = post.Likes });
        }

        // COMMENT on Post (User)
        [HttpPost("{id}/comment")]
        public IActionResult CommentOnPost(int id, [FromBody] CommentDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var post = _context.Posts.FirstOrDefault(p => p.Id == id && p.Status == "Approved");
            if (post == null) return NotFound("Post not found or not approved yet.");

            var comment = new Comment
            {
                Text = dto.Text,
                UserId = userId,
                PostId = id
            };

            _context.Comments.Add(comment);
            _context.SaveChanges();

            return Ok(new { message = "Comment added successfully", comment });
        }


        // GET Comments for a Post
        [HttpGet("{id}/comments")]
        public IActionResult GetComments(int id)
        {
            var comments = _context.Comments
                .Where(c => c.PostId == id)
                .Include(c => c.User)  // Load User info
                .Include(c => c.Post)  // Load Post info (optional)
                .ToList();

            return Ok(comments);
        }





        // SEARCH Posts by content or username
        [HttpGet("search")]
        [AllowAnonymous]
        public IActionResult SearchPosts([FromQuery] string query)
        {
            var results = _context.Posts
                .Where(p => p.Status == "Approved" &&
                           (p.Content.Contains(query) || p.User.Username.Contains(query)))
                .Select(p => new
                {
                    p.Id,
                    p.Content,
                    p.PostImage,
                    p.Status,
                    Author = p.User.Username,
                    p.Likes
                })
                .ToList();

            return Ok(results);
        }



    }
}
