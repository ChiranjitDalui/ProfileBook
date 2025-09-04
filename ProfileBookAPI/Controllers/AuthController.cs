using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProfileBookAPI.Data;
using ProfileBookAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProfileBookAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // REGISTER
        [HttpPost("register")]
        public IActionResult Register([FromBody] User userDto)
        {
            if (_context.Users.Any(u => u.Username == userDto.Username))
                return BadRequest("Username already exists.");

            var user = new User
            {
                Username = userDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.PasswordHash),
                Role = string.IsNullOrEmpty(userDto.Role) ? "User" : userDto.Role
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Auto-create blank profile
            var profile = new Profile
            {
                UserId = user.Id,
                FullName = "",
                Email = "",
                Phone = "",
                Bio = ""
            };

            _context.Profiles.Add(profile);
            _context.SaveChanges();

            return Ok(new { message = "User registered successfully with default profile!" });
        }



        // LOGIN
        [HttpPost("login")]
        public IActionResult Login([FromBody] User loginDto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == loginDto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.PasswordHash, user.PasswordHash))
                return Unauthorized("Invalid username or password.");

            // Generate JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("ThisIsASuperStrongSecretKeyForJWT123456789!");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Add userId claim
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                token = tokenHandler.WriteToken(token),
                username = user.Username,
                role = user.Role
            });
        }
    }
}
