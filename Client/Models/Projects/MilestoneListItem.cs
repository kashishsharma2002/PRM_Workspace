namespace Client.Models.Projects;

public class MilestoneListItem
{
    public long Id { get; set; }
    public string MilestoneTitle { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int StoryPoints { get; set; }
    public string MilestoneStatus { get; set; } = string.Empty;
    public short SortOrder { get; set; }
}
