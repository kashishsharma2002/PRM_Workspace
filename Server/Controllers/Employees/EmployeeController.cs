using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Common.Roles;
using Server.Models.DTOs.Employees;

namespace Server.Controllers.Employees;

[ApiController]
[Route("api/employees")]
public class EmployeeController(IEmployeeService employeeService) : ControllerBase
{
    [Authorize(Roles = RoleConstants.Manager)]
    [HttpGet("my-team")]
    public async Task<ActionResult<ApiResponse<TeamDashboardDto>>> GetMyTeam(CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await employeeService.GetTeamDashboardAsync(managerUserId, cancellationToken);
        return Ok(ApiResponse<TeamDashboardDto>.Ok(result, "Team dashboard retrieved."));
    }

    [Authorize(Roles = RoleConstants.Manager)]
    [HttpGet("my-team/{id:long}")]
    public async Task<ActionResult<ApiResponse<TeamMemberDetailDto>>> GetMyTeamMember(
        long id,
        CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await employeeService.GetTeamMemberDetailAsync(managerUserId, id, cancellationToken);
        return Ok(ApiResponse<TeamMemberDetailDto>.Ok(result, "Team member retrieved."));
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<EmployeeListResponseDto>>> GetAllEmployees(
        [FromQuery] string? status,
        [FromQuery] string? department,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.GetAllEmployeesAsync(status, department, cancellationToken);
        return Ok(ApiResponse<EmployeeListResponseDto>.Ok(result, "Employees retrieved."));
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> GetEmployee(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.GetEmployeeByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<EmployeeDetailDto>.Ok(result, "Employee retrieved."));
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateEmployee(
        long id,
        [FromBody] UpdateEmployeeRequestDto request,
        CancellationToken cancellationToken)
    {
        await employeeService.UpdateEmployeeAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Employee updated."));
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPut("{id:long}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateEmployee(
        long id,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        await employeeService.DeactivateEmployeeAsync(actorUserId, id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Employee deactivated."));
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPost("{id:long}/skills")]
    public async Task<ActionResult<ApiResponse<object>>> AddSkill(
        long id,
        [FromBody] AddSkillRequestDto request,
        CancellationToken cancellationToken)
    {
        await employeeService.AddSkillAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Skill added."));
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPut("{id:long}/skills/{skillId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateSkillProficiency(
        long id,
        long skillId,
        [FromBody] UpdateSkillProficiencyRequestDto request,
        CancellationToken cancellationToken)
    {
        await employeeService.UpdateSkillProficiencyAsync(id, skillId, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Skill proficiency updated."));
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpDelete("{id:long}/skills/{skillId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveSkill(
        long id,
        long skillId,
        CancellationToken cancellationToken)
    {
        await employeeService.RemoveSkillAsync(id, skillId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Skill removed."));
    }

    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPut("{id:long}/manager")]
    public async Task<ActionResult<ApiResponse<object>>> AssignManager(
        long id,
        [FromBody] AssignManagerRequestDto request,
        CancellationToken cancellationToken)
    {
        await employeeService.AssignManagerAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Manager assigned."));
    }

    private long GetActorUserId()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid token.");
        return userId;
    }
}
