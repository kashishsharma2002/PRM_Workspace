namespace Server.Models.Entities;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTemporaryPassword { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateOnly? JoinedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
