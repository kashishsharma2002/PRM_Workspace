namespace Server.Models.DTOs.SkillMatching;

public class AiSkillMatchResponseDto
{
    public long ProjectId { get; set; }
    public List<AiSkillMatchItemDto> Matches { get; set; } = [];
}
