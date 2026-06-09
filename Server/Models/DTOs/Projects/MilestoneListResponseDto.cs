namespace Server.Models.DTOs.Projects;

public class MilestoneListResponseDto
{
    public string ProjectName { get; set; } = string.Empty;
    public IReadOnlyList<MilestoneListItemDto> Milestones { get; set; } = [];
    public int TotalStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public int RemainingStoryPoints { get; set; }
}
