using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Common.Errors;
using Server.Common.Roles;
using Server.Models.DTOs.Ai;
using Server.Services.Ai;
using Server.Validators.Ai;

namespace Server.Controllers.Ai;

[ApiController]
[Route("api/ai")]
public class AiInsightsController(
    IAiIntegrationService aiIntegrationService,
    IValidator<AiSkillMatchQuery> skillMatchQueryValidator,
    IValidator<TeamBuilderQuery> teamBuilderQueryValidator) : ControllerBase
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
    [HttpGet("skill-match")]
    public async Task<ActionResult<ApiResponse<AiSkillMatchResponseDto>>> GetOrganizationalSkillMatch(
        [FromQuery] string? requirement,
        CancellationToken cancellationToken)
    {
        var validation = await skillMatchQueryValidator.ValidateAsync(
            new AiSkillMatchQuery { Requirement = requirement }, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", ErrorCodes.ValidationFailed, validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var managerUserId = GetActorUserId();
        var result = await aiIntegrationService.GetOrganizationalSkillMatchAsync(managerUserId, requirement, cancellationToken);
        return Ok(ApiResponse<AiSkillMatchResponseDto>.Ok(result, "Skill match generated."));
    }

    [Authorize(Roles = RoleConstants.Manager)]
    [HttpGet("projects/{projectId:long}/skill-match")]
    public async Task<ActionResult<ApiResponse<AiSkillMatchResponseDto>>> GetSkillMatch(
        long projectId,
        [FromQuery] string? requirement,
        CancellationToken cancellationToken)
    {
        var validation = await skillMatchQueryValidator.ValidateAsync(
            new AiSkillMatchQuery { Requirement = requirement }, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", ErrorCodes.ValidationFailed, validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var managerUserId = GetActorUserId();
        var result = await aiIntegrationService.GetSkillMatchAsync(managerUserId, projectId, requirement, cancellationToken);
        return Ok(ApiResponse<AiSkillMatchResponseDto>.Ok(result, "Skill match generated."));
    }

    [Authorize(Roles = RoleConstants.Manager)]
    [HttpGet("team-builder")]
    public async Task<ActionResult<ApiResponse<TeamBuilderResponseDto>>> BuildTeam(
        [FromQuery] string? requirement,
        CancellationToken cancellationToken)
    {
        var validation = await teamBuilderQueryValidator.ValidateAsync(
            new TeamBuilderQuery { Requirement = requirement }, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", ErrorCodes.ValidationFailed, validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var managerUserId = GetActorUserId();
        var result = await aiIntegrationService.BuildTeamAsync(managerUserId, requirement, cancellationToken);
        return Ok(ApiResponse<TeamBuilderResponseDto>.Ok(result, "Team builder results generated."));
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
        return long.Parse(claim);
    }
}
