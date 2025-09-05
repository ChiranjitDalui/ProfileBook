public class UpdateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string? NewPassword { get; set; } // optional
}
