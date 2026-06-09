using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Models.DTOs.SystemConfig;
using Server.Services.Interfaces;

namespace Server.Controllers;

[Authorize(Roles = "ADMIN")]
[ApiController]
[Route("api/system-config")]
public class SystemConfigController(ISystemConfigService systemConfigService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<SystemConfigResponseDto>>> GetConfig(
        CancellationToken cancellationToken)
    {
        var result = await systemConfigService.GetConfigAsync(cancellationToken);
        return Ok(ApiResponse<SystemConfigResponseDto>.Ok(result, "Configuration retrieved."));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<object>>> UpdateConfig(
        [FromBody] UpdateSystemConfigRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        await systemConfigService.UpdateConfigAsync(actorUserId, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Configuration updated."));
    }

    private long GetActorUserId()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid token.");
        return userId;
    }
}
