namespace Server.Models.DTOs.Scheduler;

public class SchedulerHealthResultDto
{
    public int EvaluatedCount { get; set; }
    public int FailedCount { get; set; }
    public int EmailsSent { get; set; }
    public int EmailsFailed { get; set; }
    public int RiskAnalysesCompleted { get; set; }
    public int RiskAnalysesFailed { get; set; }
}
