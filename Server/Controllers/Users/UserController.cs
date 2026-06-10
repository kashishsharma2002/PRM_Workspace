using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Models.DTOs.Users;

namespace Server.Controllers.Users;

[Authorize(Roles = "ADMIN")]
[ApiController]
[Route("api/users")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateUserResponseDto>>> CreateUser(
        [FromBody] CreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var result = await userService.CreateUserAccountAsync(actorUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<CreateUserResponseDto>.Ok(result, "Account created. User must change password on first login."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserListResponseDto>>> GetAllUsers(
        CancellationToken cancellationToken)
    {
        var result = await userService.GetAllUsersAsync(cancellationToken);
        return Ok(ApiResponse<UserListResponseDto>.Ok(result, "Users retrieved."));
    }

    [HttpPut("{id:long}/reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        long id,
        [FromBody] ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        await userService.ResetPasswordAsync(actorUserId, id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Password reset. User will be prompted to change it on next login."));
    }

    [HttpPut("{id:long}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateUser(
        long id,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        await userService.DeactivateUserAsync(actorUserId, id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "User deactivated."));
    }

    [HttpPut("{id:long}/reactivate")]
    public async Task<ActionResult<ApiResponse<object>>> ReactivateUser(
        long id,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        await userService.ReactivateUserAsync(actorUserId, id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { },
            "Account reactivated. Previous allocations are NOT restored."));
    }

    private long GetActorUserId()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid token.");
        return userId;
    }
}
