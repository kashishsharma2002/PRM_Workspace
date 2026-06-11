namespace Server.Models.DTOs.SystemConfig;

public class HealthThresholdSettingsDto
{
    public decimal LowHoursThreshold { get; set; }
    public int ApproachingDeadlineDays { get; set; }
}
