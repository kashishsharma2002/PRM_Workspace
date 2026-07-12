namespace Client.Models.SystemConfig;

public class UpdateSystemConfigRequest
{
    public string? LlmProvider { get; set; }
    public string? LlmApiKey { get; set; }
    public int? SchedulerIntervalHours { get; set; }
    public int? MaxWeeklyHours { get; set; }
    public int? TimesheetDeadlineWorkingDaysAfterWeekEnd { get; set; }
}
