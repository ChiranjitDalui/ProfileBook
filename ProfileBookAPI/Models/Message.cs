using System.ComponentModel.DataAnnotations;

namespace ProfileBookAPI.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public string MessageContent { get; set; } = string.Empty;

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

        // Sender
        public int SenderId { get; set; }
        public User? Sender { get; set; }

        // Receiver
        public int ReceiverId { get; set; }
        public User? Receiver { get; set; }
    }
}
