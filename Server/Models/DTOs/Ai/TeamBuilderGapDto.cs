namespace Server.Models.DTOs.Ai;

public class TeamBuilderGapDto
{
    public string ReasonType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? AlternativeEmployeeName { get; set; }
    public string? AvailableFromDate { get; set; }
}
