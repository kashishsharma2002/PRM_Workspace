namespace Server.Models.DTOs.Users;

public class ResetPasswordRequestDto
{
    public string NewTemporaryPassword { get; set; } = string.Empty;
}
