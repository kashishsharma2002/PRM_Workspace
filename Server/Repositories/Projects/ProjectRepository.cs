using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Projects;

public class ProjectRepository(PrmDbContext context) : IProjectRepository
{
    public Task<Project?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Projects
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<long, Project>> GetByIdsAsync(
        IEnumerable<long> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<long, Project>();

        var projects = await context.Projects
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return projects.ToDictionary(p => p.Id);
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Projects.OrderBy(p => p.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Project>> GetByManagerUserIdAsync(
        long managerUserId,
        CancellationToken cancellationToken = default) =>
        await context.Projects
            .Where(p => p.ManagerUserId == managerUserId && p.IsActive)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Project>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await context.Projects
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task UpdateHealthStatusAsync(
        long projectId,
        string healthStatus,
        CancellationToken cancellationToken = default)
    {
        var project = await context.Projects
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return;

        project.HealthStatus = healthStatus;
        project.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string projectCode, CancellationToken cancellationToken = default) =>
        context.Projects.AnyAsync(p => p.ProjectCode == projectCode, cancellationToken);

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await context.Projects.AddAsync(project, cancellationToken);
    }

    public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        context.Projects.Update(project);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
