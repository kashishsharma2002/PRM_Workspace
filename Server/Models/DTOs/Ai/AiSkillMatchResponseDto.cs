namespace Server.Models.DTOs.Ai;

public class AiSkillMatchResponseDto
{
    public long ProjectId { get; set; }
    public List<AiSkillMatchItemDto> Matches { get; set; } = [];
}
