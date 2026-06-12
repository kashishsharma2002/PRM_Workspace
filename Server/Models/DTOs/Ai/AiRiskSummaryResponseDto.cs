namespace Server.Models.DTOs.Ai;

public class AiRiskSummaryResponseDto
{
    public long ProjectId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = [];
}
