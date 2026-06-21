using Server.Common.Audit;
using Server.Exceptions;
using Server.Models.DTOs.Employees;
using Server.Models.Entities;

namespace Server.Services.Employees;

public partial class EmployeeService
{
    public async Task AddSkillAsync(
        long actorUserId,
        long employeeId,
        AddSkillRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetResourceProfileOrThrowAsync(employeeId, cancellationToken);
        var employeeUser = await userRepository.GetByIdAsync(profile.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Employee user not found.");

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

        await auditService.LogCreateAsync(
            actorUserId,
            AuditEntityConstants.Employees,
            profile.Id,
            new { skillName, proficiency },
            cancellationToken,
            AuditMessageBuilder.BuildSkillChangeSummary(employeeUser.FullName, "Added", skillName));

        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSkillProficiencyAsync(
        long actorUserId,
        long employeeId,
        long skillId,
        UpdateSkillProficiencyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetResourceProfileOrThrowAsync(employeeId, cancellationToken);
        var employeeUser = await userRepository.GetByIdAsync(profile.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Employee user not found.");

        var profileSkill = await employeeSkillRepository.GetAsync(profile.UserId, skillId, cancellationToken)
            ?? throw new NotFoundAppException("Skill not found for this employee.");

        var skill = await skillRepository.GetByIdAsync(skillId, cancellationToken)
            ?? throw new NotFoundAppException("Skill not found.");

        var oldProficiency = profileSkill.ProficiencyLevel;
        profileSkill.ProficiencyLevel = request.ProficiencyLevel.Trim().ToUpperInvariant();
        await employeeSkillRepository.UpdateAsync(profileSkill, cancellationToken);

        await auditService.LogUpdateAsync(
            actorUserId,
            AuditEntityConstants.Employees,
            profile.Id,
            new { skill = skill.SkillName, proficiency = oldProficiency },
            new { skill = skill.SkillName, proficiency = profileSkill.ProficiencyLevel },
            cancellationToken,
            AuditMessageBuilder.BuildSkillChangeSummary(employeeUser.FullName, "Updated", skill.SkillName));

        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveSkillAsync(
        long actorUserId,
        long employeeId,
        long skillId,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetResourceProfileOrThrowAsync(employeeId, cancellationToken);
        var employeeUser = await userRepository.GetByIdAsync(profile.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Employee user not found.");

        var profileSkill = await employeeSkillRepository.GetAsync(profile.UserId, skillId, cancellationToken)
            ?? throw new NotFoundAppException("Skill not found for this employee.");

        var skill = await skillRepository.GetByIdAsync(skillId, cancellationToken)
            ?? throw new NotFoundAppException("Skill not found.");

        await employeeSkillRepository.RemoveAsync(profileSkill, cancellationToken);

        await auditService.LogDeactivateAsync(
            actorUserId,
            AuditEntityConstants.Employees,
            profile.Id,
            new { skill = skill.SkillName },
            new { skill = skill.SkillName, removed = true },
            cancellationToken,
            AuditMessageBuilder.BuildSkillChangeSummary(employeeUser.FullName, "Removed", skill.SkillName));

        await employeeRepository.SaveChangesAsync(cancellationToken);
    }
}
