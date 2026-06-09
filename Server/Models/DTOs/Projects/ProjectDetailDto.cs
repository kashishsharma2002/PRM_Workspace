namespace Server.Models.DTOs.Projects;

public class ProjectDetailDto
{
    public long Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty;
    public int TotalStoryPoints { get; set; }
    public int StoryPointsDone { get; set; }
    public long ManagerUserId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
}
