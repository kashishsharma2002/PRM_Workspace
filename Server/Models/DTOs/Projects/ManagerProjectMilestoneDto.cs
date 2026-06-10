namespace Server.Models.DTOs.Projects;

public class ManagerProjectMilestoneDto
{
    public string MilestoneTitle { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public string MilestoneStatus { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}
