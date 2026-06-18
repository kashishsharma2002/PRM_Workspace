namespace Server.Services.Projects;

public interface IProjectHealthService
{
    Task<Models.DTOs.Scheduler.SchedulerHealthResultDto> ProcessProjectHealthNotificationsAsync(
        CancellationToken cancellationToken = default);
}
