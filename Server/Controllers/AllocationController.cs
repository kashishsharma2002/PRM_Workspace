using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Models.DTOs.Allocations;
using Server.Services.Interfaces;

namespace Server.Controllers;

[Authorize(Roles = "ADMIN")]
[ApiController]
[Route("api/allocations")]
public class AllocationController(IAllocationService allocationService) : ControllerBase
{
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
}
