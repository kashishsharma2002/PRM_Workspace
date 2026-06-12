namespace Server.Models.Entities;

public class Permission
{
    public long Id { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
}
