using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class ProjectRepository(PrmDbContext context) : IProjectRepository
{
    public Task<Project?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Projects.OrderBy(p => p.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Project>> GetByManagerUserIdAsync(
        long managerUserId,
        CancellationToken cancellationToken = default) =>
        await context.Projects
            .Where(p => p.ManagerUserId == managerUserId && p.IsActive)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

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
