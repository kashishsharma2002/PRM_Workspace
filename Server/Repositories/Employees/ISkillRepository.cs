using Server.Models.Entities;

namespace Server.Repositories.Employees;

public interface ISkillRepository
{
    Task<Skill?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<long, Skill>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);
    Task<Skill?> GetByNameAsync(string skillName, CancellationToken cancellationToken = default);
    Task AddAsync(Skill skill, CancellationToken cancellationToken = default);
}
