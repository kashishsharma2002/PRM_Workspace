using Server.Common;
using Server.Models.DTOs.SystemConfig;
using Server.Repositories.SystemConfig;

namespace Server.Services.SystemConfig;

public class HealthThresholdProvider(ISystemConfigRepository systemConfigRepository) : IHealthThresholdProvider
{
    public async Task<HealthThresholdSettingsDto> GetThresholdsAsync(CancellationToken cancellationToken = default)
    {
        var lowHoursConfig = await systemConfigRepository.GetByKeyAsync(
            ConfigKeys.HealthLowHoursThreshold, cancellationToken);
        var approachingDaysConfig = await systemConfigRepository.GetByKeyAsync(
            ConfigKeys.HealthApproachingDeadlineDays, cancellationToken);

        var lowHoursThreshold = HealthThresholdDefaults.LowHoursRatio;
        if (lowHoursConfig is not null
            && decimal.TryParse(lowHoursConfig.ConfigValue, out var parsedLowHours)
            && parsedLowHours > 0m
            && parsedLowHours <= 1m)
        {
            lowHoursThreshold = parsedLowHours;
        }

        var approachingDeadlineDays = HealthThresholdDefaults.ApproachingDeadlineDays;
        if (approachingDaysConfig is not null
            && int.TryParse(approachingDaysConfig.ConfigValue, out var parsedDays)
            && parsedDays > 0)
        {
            approachingDeadlineDays = parsedDays;
        }

        return new HealthThresholdSettingsDto
        {
            LowHoursThreshold = lowHoursThreshold,
            ApproachingDeadlineDays = approachingDeadlineDays
        };
    }
}
