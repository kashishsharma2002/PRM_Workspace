namespace Server.Models.DTOs.Projects;

public class CreateMilestoneRequestDto
{
    public string MilestoneTitle { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int StoryPoints { get; set; }
    public short? SortOrder { get; set; }
}
