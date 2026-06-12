namespace Client.Models.Projects;

public class MilestoneListResponse
{
    public string ProjectName { get; set; } = string.Empty;
    public List<MilestoneListItem> Milestones { get; set; } = [];
    public int TotalStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public int RemainingStoryPoints { get; set; }
}
