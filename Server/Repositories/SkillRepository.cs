using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class SkillRepository(PrmDbContext context) : ISkillRepository
{
    public Task<Skill?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Skills.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Skill?> GetByNameAsync(string skillName, CancellationToken cancellationToken = default) =>
        context.Skills.FirstOrDefaultAsync(s => s.SkillName == skillName, cancellationToken);

    public async Task AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        await context.Skills.AddAsync(skill, cancellationToken);
    }
}
