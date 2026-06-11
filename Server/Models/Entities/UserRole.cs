namespace Server.Models.Entities;

public class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public long? AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; }
}
