namespace Server.Models.DTOs.Projects;

public class CreateProjectResponseDto
{
    public long ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
}
