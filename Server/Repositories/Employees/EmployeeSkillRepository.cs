using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Employees;

public class EmployeeSkillRepository(PrmDbContext context) : IEmployeeSkillRepository
{
    public async Task<IReadOnlyList<UserSkill>> GetByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default) =>
        await context.UserSkills
            .Where(us => us.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserSkill>> GetByUserIdsAsync(
        IEnumerable<long> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.UserSkills
            .Where(us => ids.Contains(us.UserId))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(long userId, long skillId, CancellationToken cancellationToken = default) =>
        context.UserSkills.AnyAsync(
            us => us.UserId == userId && us.SkillId == skillId,
            cancellationToken);

    public Task<UserSkill?> GetAsync(long userId, long skillId, CancellationToken cancellationToken = default) =>
        context.UserSkills.FirstOrDefaultAsync(
            us => us.UserId == userId && us.SkillId == skillId,
            cancellationToken);

    public async Task AddAsync(UserSkill userSkill, CancellationToken cancellationToken = default)
    {
        await context.UserSkills.AddAsync(userSkill, cancellationToken);
    }

    public Task UpdateAsync(UserSkill userSkill, CancellationToken cancellationToken = default)
    {
        context.UserSkills.Update(userSkill);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(UserSkill userSkill, CancellationToken cancellationToken = default)
    {
        context.UserSkills.Remove(userSkill);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<EmployeeSkillDetailProjection>> GetSkillDetailsByUserIdsAsync(
        IEnumerable<long> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await context.UserSkills
            .Where(us => ids.Contains(us.UserId))
            .Join(context.Skills, us => us.SkillId, s => s.Id, (us, s) => new EmployeeSkillDetailProjection(
                us.UserId,
                s.SkillName,
                s.Category,
                us.ProficiencyLevel))
            .ToListAsync(cancellationToken);
    }
}
