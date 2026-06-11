namespace Server.Models.DTOs.Users;

public class UpdateUserRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
