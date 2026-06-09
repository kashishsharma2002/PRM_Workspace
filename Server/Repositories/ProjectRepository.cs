using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class ProjectRepository(PrmDbContext context) : IProjectRepository
{
    public Task<Project?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
}
