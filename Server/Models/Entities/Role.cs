namespace Server.Models.Entities;

public class Role
{
    public long Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
