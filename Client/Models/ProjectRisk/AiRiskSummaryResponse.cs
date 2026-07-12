namespace Client.Models.ProjectRisk;

public class AiRiskSummaryResponse
{
    public long ProjectId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = [];
}
