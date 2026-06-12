using Server.Models.Entities;

namespace Server.Repositories.Employees;

public interface IEmployeeSkillRepository
{
    Task<IReadOnlyList<UserSkill>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSkill>> GetByUserIdsAsync(IEnumerable<long> userIds, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(long userId, long skillId, CancellationToken cancellationToken = default);
    Task<UserSkill?> GetAsync(long userId, long skillId, CancellationToken cancellationToken = default);
    Task AddAsync(UserSkill userSkill, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserSkill userSkill, CancellationToken cancellationToken = default);
    Task RemoveAsync(UserSkill userSkill, CancellationToken cancellationToken = default);
}
