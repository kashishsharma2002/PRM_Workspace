using Server.Models.DTOs.SystemConfig;

namespace Server.Services.SystemConfig;

public interface ISystemConfigService
{
    Task<SystemConfigResponseDto> GetConfigAsync(CancellationToken cancellationToken = default);
    Task UpdateConfigAsync(long actorUserId, UpdateSystemConfigRequestDto request, CancellationToken cancellationToken = default);
    Task<decimal> GetMaxWeeklyHoursAsync(CancellationToken cancellationToken = default);
}
