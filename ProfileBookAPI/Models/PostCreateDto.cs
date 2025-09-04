using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ProfileBookAPI.Models
{
    public class PostCreateDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;

        public IFormFile? Image { get; set; } // Optional image
    }
}
