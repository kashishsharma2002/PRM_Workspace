using Server.Models.Entities;

namespace Server.Repositories.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
