namespace Client.Models.Projects;

public class CreateProjectResponse
{
    public long ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
}
