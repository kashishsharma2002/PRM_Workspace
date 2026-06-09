namespace Server.Models.DTOs.SystemConfig;

public class UpdateSystemConfigRequestDto
{
    public string? LlmProvider { get; set; }
    public string? LlmApiKey { get; set; }
    public int? SchedulerIntervalHours { get; set; }
    public int? MaxWeeklyHours { get; set; }
}
