using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Employees;

public class SkillRepository(PrmDbContext context) : ISkillRepository
{
    public Task<Skill?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Skills.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<long, Skill>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<long, Skill>();

        var skills = await context.Skills
            .Where(s => idList.Contains(s.Id))
            .ToListAsync(cancellationToken);

        return skills.ToDictionary(s => s.Id);
    }

    public Task<Skill?> GetByNameAsync(string skillName, CancellationToken cancellationToken = default) =>
        context.Skills.FirstOrDefaultAsync(s => s.SkillName == skillName, cancellationToken);

    public async Task AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        await context.Skills.AddAsync(skill, cancellationToken);
    }
}
