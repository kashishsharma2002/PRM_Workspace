using Server.Models.DTOs.Users;

namespace Server.Services.Interfaces;

public interface IUserService
{
    Task<CreateUserResponseDto> CreateUserAccountAsync(
        long actorUserId,
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<UserListResponseDto> GetAllUsersAsync(CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        long actorUserId,
        long userId,
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default);

    Task DeactivateUserAsync(
        long actorUserId,
        long userId,
        CancellationToken cancellationToken = default);

    Task ReactivateUserAsync(
        long actorUserId,
        long userId,
        CancellationToken cancellationToken = default);
}
