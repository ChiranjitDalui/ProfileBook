using Microsoft.AspNetCore.Http;

namespace ProfileBookAPI.Models
{
    public class ProfileImageUploadDto
    {
        public IFormFile Image { get; set; }
    }
}
