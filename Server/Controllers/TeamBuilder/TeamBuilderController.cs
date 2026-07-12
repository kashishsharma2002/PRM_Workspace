using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Common.Errors;
using Server.Common.Roles;
using Server.Models.DTOs.Ai;
using Server.Services.TeamBuilder.Abstractions;
using Server.Validators.TeamBuilder;

namespace Server.Controllers.TeamBuilder;

[ApiController]
[Route("api/ai")]
public class TeamBuilderController(
    ITeamBuilderService teamBuilderService,
    IValidator<TeamBuilderQuery> teamBuilderQueryValidator) : ControllerBase
{
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
        var result = await teamBuilderService.BuildTeamAsync(managerUserId, requirement, cancellationToken);
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
