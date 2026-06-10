using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Models.DTOs.Allocations;
using Server.Services.Interfaces;

namespace Server.Controllers;

[ApiController]
[Route("api/allocations")]
public class AllocationController(IAllocationService allocationService) : ControllerBase
{
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
}
