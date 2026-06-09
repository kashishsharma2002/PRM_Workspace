namespace Server.Models.DTOs.Projects;

public class ProjectListResponseDto
{
    public IReadOnlyList<ProjectListItemDto> Projects { get; set; } = [];
}
