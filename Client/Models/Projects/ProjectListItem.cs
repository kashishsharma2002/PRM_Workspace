namespace Client.Models.Projects;

public class ProjectListItem
{
    public long Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public DateOnly EndDate { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public int StoryPointsDone { get; set; }
    public int TotalStoryPoints { get; set; }
}
