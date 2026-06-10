using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Models.DTOs.Auth;

namespace Server.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Login successful."));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> ChangePassword(
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail("Invalid token."));

        var result = await authService.ChangePasswordAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Password changed successfully."));
    }
}
