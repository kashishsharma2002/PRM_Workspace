namespace Server.Models.DTOs.Ai;

public class AiSkillMatchItemDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal RemainingCapacityPercentage { get; set; }
}
