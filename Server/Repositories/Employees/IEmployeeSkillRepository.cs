using Server.Models.Entities;

namespace Server.Repositories.Employees;

public interface IEmployeeSkillRepository
{
    Task<IReadOnlyList<EmployeeSkill>> GetByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeSkill>> GetByEmployeeIdsAsync(IEnumerable<long> employeeIds, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(long employeeId, long skillId, CancellationToken cancellationToken = default);
    Task<EmployeeSkill?> GetAsync(long employeeId, long skillId, CancellationToken cancellationToken = default);
    Task AddAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default);
    Task RemoveAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default);
}
