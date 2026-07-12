using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Common.Errors;
using Server.Common.Roles;
using Server.Models.DTOs.SkillMatching;
using Server.Services.SkillMatching.Abstractions;
using Server.Validators.SkillMatching;

namespace Server.Controllers.SkillMatching;

[ApiController]
[Route("api/ai")]
public class SkillMatchingController(
    ISkillMatchingService skillMatchingService,
    IValidator<SkillMatchQuery> skillMatchQueryValidator) : ControllerBase
{
    [Authorize(Roles = RoleConstants.Manager)]
    [HttpGet("skill-match")]
    public async Task<ActionResult<ApiResponse<AiSkillMatchResponseDto>>> GetOrganizationalSkillMatch(
        [FromQuery] string? requirement,
        CancellationToken cancellationToken)
    {
        var validation = await skillMatchQueryValidator.ValidateAsync(
            new SkillMatchQuery { Requirement = requirement }, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", ErrorCodes.ValidationFailed, validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var managerUserId = GetActorUserId();
        var result = await skillMatchingService.GetOrganizationalSkillMatchAsync(managerUserId, requirement, cancellationToken);
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
            new SkillMatchQuery { Requirement = requirement }, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", ErrorCodes.ValidationFailed, validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var managerUserId = GetActorUserId();
        var result = await skillMatchingService.GetSkillMatchAsync(
            managerUserId, projectId, requirement, cancellationToken: cancellationToken);
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
