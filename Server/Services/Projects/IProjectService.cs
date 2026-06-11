using Server.Models.DTOs.Projects;
using Server.Models.DTOs.Scheduler;

namespace Server.Services.Projects;

public interface IProjectService
{
    Task<CreateProjectResponseDto> CreateProjectAsync(long actorUserId, CreateProjectRequestDto request, CancellationToken cancellationToken = default);
    Task<ProjectListResponseDto> GetAllProjectsAsync(CancellationToken cancellationToken = default);
    Task<ProjectDetailDto> GetProjectByIdAsync(long projectId, CancellationToken cancellationToken = default);
    Task UpdateProjectAsync(long projectId, UpdateProjectRequestDto request, CancellationToken cancellationToken = default);
    Task ArchiveProjectAsync(long actorUserId, long projectId, CancellationToken cancellationToken = default);
    Task<MilestoneListResponseDto> GetMilestonesAsync(long projectId, CancellationToken cancellationToken = default);
    Task AddMilestoneAsync(long projectId, CreateMilestoneRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateMilestoneStatusAsync(long projectId, long milestoneId, UpdateMilestoneStatusRequestDto request, CancellationToken cancellationToken = default);
    Task<ManagerProjectListResponseDto> GetMyProjectsAsync(long managerUserId, CancellationToken cancellationToken = default);
    Task<ManagerProjectDetailDto> GetManagerProjectDetailAsync(long managerUserId, long projectId, CancellationToken cancellationToken = default);
    Task<SchedulerHealthResultDto> EvaluateAllProjectsHealthAsync(CancellationToken cancellationToken = default);
}
