namespace Client.HttpClients;

public class CreateProjectRequest
{
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public long ManagerUserId { get; set; }
    public int TotalStoryPoints { get; set; }
}

public class CreateProjectResponse
{
    public long ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
}

public class UpdateProjectRequest
{
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public long ManagerUserId { get; set; }
    public int TotalStoryPoints { get; set; }
}

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

public class ProjectListResponse
{
    public List<ProjectListItem> Projects { get; set; } = [];
}

public class ProjectDetail
{
    public long Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public int TotalStoryPoints { get; set; }
    public long ManagerUserId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
}

public class CreateMilestoneRequest
{
    public string MilestoneTitle { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int StoryPoints { get; set; }
}

public class UpdateMilestoneStatusRequest
{
    public string MilestoneStatus { get; set; } = string.Empty;
}

public class MilestoneListItem
{
    public long Id { get; set; }
    public string MilestoneTitle { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int StoryPoints { get; set; }
    public string MilestoneStatus { get; set; } = string.Empty;
    public short SortOrder { get; set; }
}

public class MilestoneListResponse
{
    public string ProjectName { get; set; } = string.Empty;
    public List<MilestoneListItem> Milestones { get; set; } = [];
    public int TotalStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public int RemainingStoryPoints { get; set; }
}
