using Server.Models.DTOs.Auth;

namespace Server.Services.Auth;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<LoginResponseDto> ChangePasswordAsync(long userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
}
