using Server.Models.DTOs.Users;

namespace Server.Services.Interfaces;

public interface IUserService
{
    Task<CreateUserResponseDto> CreateUserAccountAsync(
        long actorUserId,
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default);
}
