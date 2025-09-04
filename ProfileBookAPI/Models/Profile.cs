using System.ComponentModel.DataAnnotations;

namespace ProfileBookAPI.Models
{
    public class Profile
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string? ProfileImage { get; set; } // store file path


        // Foreign Key
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
