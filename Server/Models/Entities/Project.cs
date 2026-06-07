namespace Server.Models.Entities;

public class Project
{
    public long Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = "GREEN";
    public int TotalStoryPoints { get; set; }
    public long ManagerUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
