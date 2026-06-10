using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Models.DTOs.Allocations;

namespace Server.Controllers.Allocations;

[ApiController]
[Route("api/allocations")]
public class AllocationController(IAllocationService allocationService) : ControllerBase
{
    [Authorize(Roles = "MANAGER")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateAllocationResponseDto>>> CreateAllocation(
        [FromBody] CreateAllocationRequestDto request,
        CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await allocationService.CreateAllocationAsync(managerUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<CreateAllocationResponseDto>.Ok(result, "Allocation created."));
    }

    [Authorize(Roles = "MANAGER")]
    [HttpPut("{id:long}/end")]
    public async Task<ActionResult<ApiResponse<EndAllocationResponseDto>>> EndAllocation(
        long id,
        CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await allocationService.EndAllocationAsync(managerUserId, id, cancellationToken);
        return Ok(ApiResponse<EndAllocationResponseDto>.Ok(result, "Allocation ended."));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AllocationListResponseDto>>> GetAllAllocations(
        [FromQuery] long? employeeId,
        [FromQuery] long? projectId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await allocationService.GetAllAllocationsAsync(employeeId, projectId, status, cancellationToken);
        return Ok(ApiResponse<AllocationListResponseDto>.Ok(result, "Allocations retrieved."));
    }

    [Authorize(Roles = "EMPLOYEE")]
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<EmployeeAllocationListResponseDto>>> GetMyAllocations(
        CancellationToken cancellationToken)
    {
        var employeeIdClaim = User.FindFirstValue("employee_id")
            ?? throw new UnauthorizedAccessException("Employee ID not found in token.");
        var employeeId = long.Parse(employeeIdClaim);
        var result = await allocationService.GetMyAllocationsAsync(employeeId, cancellationToken);
        return Ok(ApiResponse<EmployeeAllocationListResponseDto>.Ok(result, "Allocations retrieved."));
    }

    private long GetActorUserId()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid token.");
        return userId;
    }
}
