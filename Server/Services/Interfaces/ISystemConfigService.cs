using Server.Models.DTOs.SystemConfig;

namespace Server.Services.Interfaces;

public interface ISystemConfigService
{
    Task<SystemConfigResponseDto> GetConfigAsync(CancellationToken cancellationToken = default);
    Task UpdateConfigAsync(long actorUserId, UpdateSystemConfigRequestDto request, CancellationToken cancellationToken = default);
}
