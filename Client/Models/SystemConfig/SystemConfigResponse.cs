namespace Client.Models.SystemConfig;

public class SystemConfigResponse
{
    public string LlmProvider { get; set; } = string.Empty;
    public string LlmApiKeyMasked { get; set; } = string.Empty;
    public int SchedulerIntervalHours { get; set; }
    public int MaxWeeklyHours { get; set; }
    public int TimesheetDeadlineWorkingDaysAfterWeekEnd { get; set; }
}
