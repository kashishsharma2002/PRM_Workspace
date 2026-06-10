using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Models.DTOs.Projects;
using Server.Services.Interfaces;

namespace Server.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectController(IProjectService projectService) : ControllerBase
{
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateProjectResponseDto>>> CreateProject(
        [FromBody] CreateProjectRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var result = await projectService.CreateProjectAsync(actorUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<CreateProjectResponseDto>.Ok(result, "Project created."));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ProjectListResponseDto>>> GetAllProjects(
        CancellationToken cancellationToken)
    {
        var result = await projectService.GetAllProjectsAsync(cancellationToken);
        return Ok(ApiResponse<ProjectListResponseDto>.Ok(result, "Projects retrieved."));
    }

    [Authorize(Roles = "MANAGER")]
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<ManagerProjectListResponseDto>>> GetMyProjects(
        CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await projectService.GetMyProjectsAsync(managerUserId, cancellationToken);
        return Ok(ApiResponse<ManagerProjectListResponseDto>.Ok(result, "Projects retrieved."));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<ProjectDetailDto>>> GetProject(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await projectService.GetProjectByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ProjectDetailDto>.Ok(result, "Project retrieved."));
    }

    [Authorize(Roles = "MANAGER")]
    [HttpGet("{id:long}/manager")]
    public async Task<ActionResult<ApiResponse<ManagerProjectDetailDto>>> GetManagerProject(
        long id,
        CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await projectService.GetManagerProjectDetailAsync(managerUserId, id, cancellationToken);
        return Ok(ApiResponse<ManagerProjectDetailDto>.Ok(result, "Project detail retrieved."));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateProject(
        long id,
        [FromBody] UpdateProjectRequestDto request,
        CancellationToken cancellationToken)
    {
        await projectService.UpdateProjectAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Project updated."));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet("{id:long}/milestones")]
    public async Task<ActionResult<ApiResponse<MilestoneListResponseDto>>> GetMilestones(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await projectService.GetMilestonesAsync(id, cancellationToken);
        return Ok(ApiResponse<MilestoneListResponseDto>.Ok(result, "Milestones retrieved."));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost("{id:long}/milestones")]
    public async Task<ActionResult<ApiResponse<object>>> AddMilestone(
        long id,
        [FromBody] CreateMilestoneRequestDto request,
        CancellationToken cancellationToken)
    {
        await projectService.AddMilestoneAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<object>.Ok(new { }, "Milestone added."));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:long}/milestones/{milestoneId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMilestoneStatus(
        long id,
        long milestoneId,
        [FromBody] UpdateMilestoneStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        await projectService.UpdateMilestoneStatusAsync(id, milestoneId, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Milestone status updated."));
    }

    private long GetActorUserId()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid token.");
        return userId;
    }
}
