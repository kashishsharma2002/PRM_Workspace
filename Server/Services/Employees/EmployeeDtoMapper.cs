using Server.Models.DTOs.Employees;
using Server.Models.Entities;

namespace Server.Services.Employees;

public static class EmployeeDtoMapper
{
    public static List<EmployeeSkillDto> MapSkills(
        IEnumerable<UserSkill> profileSkills,
        IReadOnlyDictionary<long, Skill> skillsById) =>
        profileSkills
            .Where(ps => skillsById.ContainsKey(ps.SkillId))
            .Select(ps =>
            {
                var skill = skillsById[ps.SkillId];
                return new EmployeeSkillDto
                {
                    SkillId = skill.Id,
                    SkillName = skill.SkillName,
                    Category = skill.Category,
                    ProficiencyLevel = ps.ProficiencyLevel
                };
            })
            .ToList();

    public static List<ActiveAllocationDto> MapActiveAllocations(
        IEnumerable<ProjectAllocation> allocations,
        IReadOnlyDictionary<long, Project> projectsById) =>
        allocations.Select(allocation => new ActiveAllocationDto
        {
            AllocationId = allocation.Id,
            ProjectName = projectsById.TryGetValue(allocation.ProjectId, out var project)
                ? project.ProjectName
                : "Unknown",
            AllocationPercentage = allocation.AllocationPercentage,
            AllocationStartDate = allocation.AllocationStartDate,
            AllocationEndDate = allocation.AllocationEndDate
        }).ToList();
}
