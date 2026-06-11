using Server.Models.DTOs.SystemConfig;

namespace Server.Services.SystemConfig;

public interface IHealthThresholdProvider
{
    Task<HealthThresholdSettingsDto> GetThresholdsAsync(CancellationToken cancellationToken = default);
}
