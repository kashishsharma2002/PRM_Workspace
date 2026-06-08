using Server.Models.DTOs.Auth;

namespace Server.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(long userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
}
