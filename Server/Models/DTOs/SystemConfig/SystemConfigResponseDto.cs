namespace Server.Models.DTOs.SystemConfig;

public class SystemConfigResponseDto
{
    public string LlmProvider { get; set; } = string.Empty;
    public string LlmApiKeyMasked { get; set; } = string.Empty;
    public int SchedulerIntervalHours { get; set; }
    public int MaxWeeklyHours { get; set; }
}
