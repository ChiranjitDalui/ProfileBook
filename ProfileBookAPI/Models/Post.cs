using System.ComponentModel.DataAnnotations;

namespace ProfileBookAPI.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? PostImage { get; set; } // store file path if image uploaded

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        // Relationships
        public int UserId { get; set; }
        public User? User { get; set; }

        // Likes & Comments
        public int Likes { get; set; } = 0;
        public List<Comment>? Comments { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User? User { get; set; }

        public int PostId { get; set; }
        public Post? Post { get; set; }
    }
}
