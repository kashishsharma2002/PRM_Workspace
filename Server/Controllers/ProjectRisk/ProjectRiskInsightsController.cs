using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Common.Roles;
using Server.Models.DTOs.ProjectRisk;
using Server.Services.ProjectRisk.Abstractions;

namespace Server.Controllers.ProjectRisk;

[ApiController]
[Route("api/ai")]
public class ProjectRiskInsightsController(IProjectRiskInsightsService projectRiskInsightsService) : ControllerBase
{
    [Authorize(Roles = RoleConstants.Manager)]
    [HttpGet("projects/{projectId:long}/risk-summary")]
    public async Task<ActionResult<ApiResponse<AiRiskSummaryResponseDto>>> GetRiskSummary(
        long projectId,
        CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await projectRiskInsightsService.GetRiskSummaryAsync(managerUserId, projectId, cancellationToken);
        return Ok(ApiResponse<AiRiskSummaryResponseDto>.Ok(result, "Risk summary generated."));
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
        return long.Parse(claim);
    }
}
