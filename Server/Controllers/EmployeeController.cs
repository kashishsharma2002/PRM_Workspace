using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Models.DTOs.Employees;
using Server.Services.Interfaces;

namespace Server.Controllers;

[Authorize(Roles = "ADMIN")]
[ApiController]
[Route("api/employees")]
public class EmployeeController(IEmployeeService employeeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<EmployeeListResponseDto>>> GetAllEmployees(
        [FromQuery] string? status,
        [FromQuery] string? department,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.GetAllEmployeesAsync(status, department, cancellationToken);
        return Ok(ApiResponse<EmployeeListResponseDto>.Ok(result, "Employees retrieved."));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> GetEmployee(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.GetEmployeeByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<EmployeeDetailDto>.Ok(result, "Employee retrieved."));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateEmployee(
        long id,
        [FromBody] UpdateEmployeeRequestDto request,
        CancellationToken cancellationToken)
    {
        await employeeService.UpdateEmployeeAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Employee updated."));
    }

    [HttpPut("{id:long}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateEmployee(
        long id,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        await employeeService.DeactivateEmployeeAsync(actorUserId, id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Employee deactivated."));
    }

    [HttpPost("{id:long}/skills")]
    public async Task<ActionResult<ApiResponse<object>>> AddSkill(
        long id,
        [FromBody] AddSkillRequestDto request,
        CancellationToken cancellationToken)
    {
        await employeeService.AddSkillAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Skill added."));
    }

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

    [HttpDelete("{id:long}/skills/{skillId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveSkill(
        long id,
        long skillId,
        CancellationToken cancellationToken)
    {
        await employeeService.RemoveSkillAsync(id, skillId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Skill removed."));
    }

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
