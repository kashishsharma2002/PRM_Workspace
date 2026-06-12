using Server.Exceptions;
using Server.Models.DTOs.Employees;
using Server.Models.Entities;

namespace Server.Services.Employees;

public partial class EmployeeService
{
    public async Task AddSkillAsync(
        long employeeId,
        AddSkillRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetResourceProfileOrThrowAsync(employeeId, cancellationToken);

        var skillName = request.SkillName.Trim();
        var category = request.Category.Trim().ToUpperInvariant();
        var proficiency = request.ProficiencyLevel.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        var skill = await skillRepository.GetByNameAsync(skillName, cancellationToken);
        if (skill is null)
        {
            skill = new Skill
            {
                SkillName = skillName,
                Category = category,
                IsActive = true,
                CreatedAt = now
            };
            await skillRepository.AddAsync(skill, cancellationToken);
            await employeeRepository.SaveChangesAsync(cancellationToken);
        }

        if (await employeeSkillRepository.ExistsAsync(profile.UserId, skill.Id, cancellationToken))
            throw new ConflictAppException("Employee already has this skill.");

        await employeeSkillRepository.AddAsync(new UserSkill
        {
            UserId = profile.UserId,
            SkillId = skill.Id,
            ProficiencyLevel = proficiency,
            CreatedAt = now
        }, cancellationToken);

        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSkillProficiencyAsync(
        long employeeId,
        long skillId,
        UpdateSkillProficiencyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetResourceProfileOrThrowAsync(employeeId, cancellationToken);

        var profileSkill = await employeeSkillRepository.GetAsync(profile.UserId, skillId, cancellationToken)
            ?? throw new NotFoundAppException("Skill not found for this employee.");

        profileSkill.ProficiencyLevel = request.ProficiencyLevel.Trim().ToUpperInvariant();
        await employeeSkillRepository.UpdateAsync(profileSkill, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveSkillAsync(long employeeId, long skillId, CancellationToken cancellationToken = default)
    {
        var profile = await GetResourceProfileOrThrowAsync(employeeId, cancellationToken);

        var profileSkill = await employeeSkillRepository.GetAsync(profile.UserId, skillId, cancellationToken)
            ?? throw new NotFoundAppException("Skill not found for this employee.");

        await employeeSkillRepository.RemoveAsync(profileSkill, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);
    }
}
