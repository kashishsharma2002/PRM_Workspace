using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class EmployeeSkillRepository(PrmDbContext context) : IEmployeeSkillRepository
{
    public async Task<IReadOnlyList<EmployeeSkill>> GetByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default) =>
        await context.EmployeeSkills
            .Where(es => es.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(long employeeId, long skillId, CancellationToken cancellationToken = default) =>
        context.EmployeeSkills.AnyAsync(es => es.EmployeeId == employeeId && es.SkillId == skillId, cancellationToken);

    public Task<EmployeeSkill?> GetAsync(long employeeId, long skillId, CancellationToken cancellationToken = default) =>
        context.EmployeeSkills.FirstOrDefaultAsync(es => es.EmployeeId == employeeId && es.SkillId == skillId, cancellationToken);

    public async Task AddAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default)
    {
        await context.EmployeeSkills.AddAsync(employeeSkill, cancellationToken);
    }

    public Task UpdateAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default)
    {
        context.EmployeeSkills.Update(employeeSkill);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken = default)
    {
        context.EmployeeSkills.Remove(employeeSkill);
        return Task.CompletedTask;
    }
}
