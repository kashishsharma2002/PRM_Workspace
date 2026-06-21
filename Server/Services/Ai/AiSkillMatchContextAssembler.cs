using Server.Models.DTOs.Ai.Context;
using Server.Services.Ai.Abstractions;

namespace Server.Services.Ai;

public class AiSkillMatchContextAssembler : IAiSkillMatchContextAssembler
{
    public List<AiSkillMatchCandidateContext> AssembleCandidates(AiSkillMatchRawData rawData)
    {
        var profileByUserId = rawData.ResourceProfiles.ToDictionary(rp => rp.UserId, rp => rp.Id);
        var allocationsByProfileId = rawData.ActiveAllocations
            .GroupBy(a => a.ResourceProfileId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return rawData.Employees.Select(emp =>
        {
            var profileId = profileByUserId.GetValueOrDefault(emp.Id);
            var empAllocations = profileId > 0 && allocationsByProfileId.TryGetValue(profileId, out var allocs)
                ? allocs.Select(a => new AiAllocationContext
                {
                    ProjectId = a.ProjectId,
                    EmployeeName = emp.FullName,
                    AllocationPercentage = a.AllocationPercentage,
                    StartDate = a.AllocationStartDate.ToString("yyyy-MM-dd"),
                    EndDate = a.AllocationEndDate.ToString("yyyy-MM-dd"),
                    ProjectName = a.ProjectName
                }).ToList()
                : [];

            var totalAllocated = empAllocations.Sum(a => a.AllocationPercentage);
            var remaining = Math.Max(0, 100 - totalAllocated);

            return new AiSkillMatchCandidateContext
            {
                EmployeeId = emp.Id,
                FullName = emp.FullName,
                Designation = emp.Designation,
                Department = emp.Department,
                Skills = rawData.UserSkills
                    .Where(s => s.UserId == emp.Id)
                    .Select(s => new AiSkillContext
                    {
                        SkillName = s.SkillName,
                        Category = s.Category,
                        ProficiencyLevel = s.ProficiencyLevel
                    })
                    .ToList(),
                ActiveAllocations = empAllocations,
                TotalAllocatedPercentage = totalAllocated,
                RemainingCapacityPercentage = remaining
            };
        }).ToList();
    }
}
