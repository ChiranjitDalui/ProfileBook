using System.ComponentModel.DataAnnotations;

namespace ProfileBookAPI.Models
{
    public class Group
    {
        public int Id { get; set; }

        [Required]
        public string GroupName { get; set; } = string.Empty;

        public List<GroupMember>? Members { get; set; }
    }

    public class GroupMember
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public Group? Group { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
