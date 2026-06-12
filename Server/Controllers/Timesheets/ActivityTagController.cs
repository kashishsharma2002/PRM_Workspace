using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Common.Roles;
using Server.Models.DTOs.Timesheets;

namespace Server.Controllers.Timesheets;

[Authorize(Roles = RoleConstants.Employee)]
[ApiController]
[Route("api/activity-tags")]
public class ActivityTagController(ITimesheetService timesheetService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ActivityTagDto>>>> GetActivityTags(
        CancellationToken cancellationToken)
    {
        var result = await timesheetService.GetActivityTagsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ActivityTagDto>>.Ok(result, "Activity tags retrieved."));
    }
}
