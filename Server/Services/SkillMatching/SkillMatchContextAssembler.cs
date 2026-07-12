using Server.Common.Roles;
using Server.Models.DTOs.SkillMatching.Context;
using Server.Repositories.Allocations;
using Server.Repositories.Employees;
using Server.Repositories.Users;
using Server.Services.SkillMatching.Abstractions;

namespace Server.Services.SkillMatching;

public class SkillMatchContextAssembler(
    IUserRepository userRepository,
    IEmployeeSkillRepository employeeSkillRepository,
    IEmployeeRepository employeeRepository,
    IAllocationRepository allocationRepository) : ISkillMatchContextAssembler
{
    public async Task<AiSkillMatchRawData> LoadRawDataAsync(CancellationToken cancellationToken = default)
    {
        var employees = await userRepository.GetActiveUsersByRoleAsync(RoleConstants.Employee, cancellationToken);
        var employeeIds = employees.Select(e => e.Id).ToList();

        var skillDetails = await employeeSkillRepository.GetSkillDetailsByUserIdsAsync(employeeIds, cancellationToken);
        var userSkills = skillDetails.Select(s => new AiUserSkillRaw
        {
            UserId = s.UserId,
            SkillName = s.SkillName,
            Category = s.Category,
            ProficiencyLevel = s.ProficiencyLevel
        }).ToList();

        var resourceProfiles = (await employeeRepository
            .GetResourceProfilesByUserIdsAsync(employeeIds, cancellationToken)).ToList();

        var profileIds = resourceProfiles.Select(rp => rp.Id).ToList();
        var allocationDetails = profileIds.Count == 0
            ? []
            : await allocationRepository.GetActiveWithProjectNamesByProfileIdsAsync(profileIds, cancellationToken);

        var activeAllocations = allocationDetails.Select(a => new AiActiveAllocationRaw
        {
            ProjectId = a.ProjectId,
            ResourceProfileId = a.ResourceProfileId,
            AllocationPercentage = a.AllocationPercentage,
            AllocationStartDate = a.AllocationStartDate,
            AllocationEndDate = a.AllocationEndDate,
            ProjectName = a.ProjectName
        }).ToList();

        return new AiSkillMatchRawData
        {
            Employees = employees.ToList(),
            UserSkills = userSkills,
            ResourceProfiles = resourceProfiles,
            ActiveAllocations = activeAllocations
        };
    }

    public async Task<List<AiSkillMatchCandidateContext>> AssembleCandidatesAsync(
        CancellationToken cancellationToken = default)
    {
        var rawData = await LoadRawDataAsync(cancellationToken);
        return AssembleCandidates(rawData);
    }

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
