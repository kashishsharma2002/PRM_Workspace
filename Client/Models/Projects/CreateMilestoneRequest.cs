namespace Client.Models.Projects;

public class CreateMilestoneRequest
{
    public string MilestoneTitle { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int StoryPoints { get; set; }
}
