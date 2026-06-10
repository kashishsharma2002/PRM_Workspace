using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Common.Roles;
using Server.Models.DTOs.Ai;
using Server.Services.Ai;

namespace Server.Controllers.Ai;

[ApiController]
[Route("api/ai")]
public class AiInsightsController(IAiIntegrationService aiIntegrationService) : ControllerBase
{
    [Authorize(Roles = RoleConstants.Manager)]
    [HttpGet("projects/{projectId:long}/risk-summary")]
    public async Task<ActionResult<ApiResponse<AiRiskSummaryResponseDto>>> GetRiskSummary(
        long projectId,
        CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await aiIntegrationService.GetRiskSummaryAsync(managerUserId, projectId, cancellationToken);
        return Ok(ApiResponse<AiRiskSummaryResponseDto>.Ok(result, "Risk summary generated."));
    }

    [Authorize(Roles = RoleConstants.Manager)]
    [HttpGet("projects/{projectId:long}/skill-match")]
    public async Task<ActionResult<ApiResponse<AiSkillMatchResponseDto>>> GetSkillMatch(
        long projectId,
        CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await aiIntegrationService.GetSkillMatchAsync(managerUserId, projectId, cancellationToken);
        return Ok(ApiResponse<AiSkillMatchResponseDto>.Ok(result, "Skill match generated."));
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
        return long.Parse(claim);
    }
}
