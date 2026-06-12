namespace Client.Models.Ai;

public class AiSkillMatchResponse
{
    public long ProjectId { get; set; }
    public List<AiSkillMatchItem> Matches { get; set; } = [];
}
