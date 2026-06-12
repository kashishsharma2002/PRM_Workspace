namespace Client.Models.Users;

public class ResetPasswordRequest
{
    public string NewTemporaryPassword { get; set; } = string.Empty;
}
