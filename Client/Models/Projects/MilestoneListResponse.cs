namespace Client.Models.Projects;

public class MilestoneListResponse
{
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<MilestoneListItem> Milestones { get; set; } = [];
    public int TotalStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public int RemainingStoryPoints { get; set; }
}
