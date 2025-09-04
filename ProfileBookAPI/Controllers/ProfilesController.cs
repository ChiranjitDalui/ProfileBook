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
    [Authorize] // Require JWT for all routes
    public class ProfilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfilesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // CREATE Profile
        [HttpPost]
        public IActionResult CreateProfile([FromBody] Profile profile)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            profile.UserId = userId; // link to logged-in user

            _context.Profiles.Add(profile);
            _context.SaveChanges();
            return Ok(profile);
        }

        // GET All Profiles (Admin only)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetProfiles()
        {
            return Ok(_context.Profiles.ToList());
        }

        // GET Own Profile
        [HttpGet("me")]
        public IActionResult GetMyProfile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = _context.Profiles.FirstOrDefault(p => p.UserId == userId);
            if (profile == null) return NotFound("Profile not found");
            return Ok(profile);
        }

        // UPDATE Own Profile
        [HttpPut("me")]
        public IActionResult UpdateMyProfile([FromBody] Profile updatedProfile)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = _context.Profiles.FirstOrDefault(p => p.UserId == userId);
            if (profile == null) return NotFound();

            profile.FullName = updatedProfile.FullName;
            profile.Email = updatedProfile.Email;
            profile.Phone = updatedProfile.Phone;
            profile.Bio = updatedProfile.Bio;

            _context.SaveChanges();
            return Ok(profile);
        }

        // DELETE Own Profile
        [HttpDelete("me")]
        public IActionResult DeleteMyProfile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = _context.Profiles.FirstOrDefault(p => p.UserId == userId);
            if (profile == null) return NotFound();

            _context.Profiles.Remove(profile);
            _context.SaveChanges();
            return Ok("Profile deleted successfully.");
        }

        // DELETE Any User's Profile (Admin Only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteProfileByAdmin(int id)
        {
            var profile = _context.Profiles.FirstOrDefault(p => p.Id == id);
            if (profile == null) return NotFound("Profile not found.");

            _context.Profiles.Remove(profile);
            _context.SaveChanges();
            return Ok($"Profile with ID {id} deleted by Admin.");
        }

        // SEARCH Users by name/Username/email/phone
        [HttpGet("search")]
        [AllowAnonymous]
        public IActionResult SearchUsers([FromQuery] string query)
        {
            query ??= string.Empty;
            query = query.Trim();

            // LEFT JOIN Users -> Profiles so users without a profile are also returned
            var results = (from u in _context.Users
                           join p in _context.Profiles on u.Id equals p.UserId into prof
                           from p in prof.DefaultIfEmpty() // LEFT JOIN

                           where !string.IsNullOrEmpty(query) && (
                                EF.Functions.Like(u.Username, $"%{query}%") ||
                                EF.Functions.Like((p != null ? p.FullName : ""), $"%{query}%") ||
                                EF.Functions.Like((p != null ? p.Email : ""), $"%{query}%") ||
                                EF.Functions.Like((p != null ? p.Phone : ""), $"%{query}%")
                           )

                           select new
                           {
                               UserId = u.Id,
                               u.Username,
                               Role = u.Role,

                               // Profile (may be null if not created yet)
                               ProfileId = p != null ? p.Id : (int?)null,
                               FullName = p != null ? p.FullName : null,
                               Email = p != null ? p.Email : null,
                               Phone = p != null ? p.Phone : null,
                               ProfileImage = p != null ? p.ProfileImage : null
                           })
                           .OrderBy(x => x.Username)
                           .Take(50) // guard against huge responses
                           .ToList();

            return Ok(results);
        }



        // UPLOAD Profile Picture
        [HttpPost("me/upload-image")]
        public IActionResult UploadProfileImage([FromForm] ProfileImageUploadDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = _context.Profiles.FirstOrDefault(p => p.UserId == userId);
            if (profile == null) return NotFound("Profile not found.");

            if (dto.Image == null || dto.Image.Length == 0)
                return BadRequest("No image uploaded.");

            string uploads = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
            string filePath = Path.Combine("uploads", fileName);

            using (var stream = new FileStream(Path.Combine(_env.WebRootPath, filePath), FileMode.Create))
            {
                dto.Image.CopyTo(stream);
            }

            profile.ProfileImage = filePath;
            _context.SaveChanges();

            return Ok(new { message = "Profile picture uploaded successfully!", profile.ProfileImage });
        }


    }
}
