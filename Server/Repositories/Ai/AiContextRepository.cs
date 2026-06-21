using Microsoft.EntityFrameworkCore;

using Server.Common.Ai;

using Server.Common.Allocations;

using Server.Common.Projects;

using Server.Common.Roles;

using Server.Data;

using Server.Models.DTOs.Ai.Context;



namespace Server.Repositories.Ai;



public class AiContextRepository(PrmDbContext context) : IAiContextRepository

{

    public async Task<AiRiskContextModel?> GetRiskContextAsync(long projectId, CancellationToken cancellationToken = default)

    {

        var project = await context.Projects.FindAsync([projectId], cancellationToken);

        if (project is null)

            return null;



        var milestones = await context.ProjectMilestones

            .Where(m => m.ProjectId == projectId)

            .OrderBy(m => m.SortOrder)

            .ToListAsync(cancellationToken);



        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var allocations = await context.ProjectAllocations

            .Where(a => a.ProjectId == projectId && a.AllocationStatus == AllocationStatusConstants.Active)

            .Join(context.ResourceProfiles, a => a.ResourceProfileId, rp => rp.Id, (a, rp) => new { a, rp })

            .Join(context.Users, combined => combined.rp.UserId, u => u.Id, (combined, u) => new AiAllocationContext

            {

                EmployeeName = u.FullName,

                AllocationPercentage = combined.a.AllocationPercentage,

                StartDate = combined.a.AllocationStartDate.ToString("yyyy-MM-dd"),

                EndDate = combined.a.AllocationEndDate.ToString("yyyy-MM-dd")

            })

            .ToListAsync(cancellationToken);



        var fourWeeksAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-AiValidationLimits.RecentTimesheetWindowDays));

        var timesheetHoursRaw = await context.TimesheetLineItems

            .Where(li => li.ProjectId == projectId && li.WorkDate >= fourWeeksAgo)

            .Join(context.Timesheets, li => li.TimesheetId, t => t.Id, (li, t) => new { li, t })

            .Join(context.ResourceProfiles, combined => combined.t.ResourceProfileId, rp => rp.Id, (combined, rp) => new { combined.li, rp })

            .Join(context.Users, combined => combined.rp.UserId, u => u.Id, (combined, u) => new

            {

                u.FullName,

                combined.li.HoursLogged,

                combined.li.WorkDate

            })

            .ToListAsync(cancellationToken);



        var timesheetHours = timesheetHoursRaw.Select(t => new AiTimesheetHoursContext

        {

            EmployeeName = t.FullName,

            HoursLogged = t.HoursLogged,

            WorkDate = t.WorkDate.HasValue ? t.WorkDate.Value.ToString("yyyy-MM-dd") : null

        }).ToList();



        return new AiRiskContextModel

        {

            ProjectId = project.Id,

            ProjectName = project.ProjectName,

            ProjectCode = project.ProjectCode,

            Description = project.Description,

            ProjectStatus = project.ProjectStatus,

            HealthStatus = project.HealthStatus,

            StartDate = project.StartDate.ToString("yyyy-MM-dd"),

            EndDate = project.EndDate.ToString("yyyy-MM-dd"),

            Milestones = milestones.Select(m => new AiMilestoneContext

            {

                Title = m.MilestoneTitle,

                DueDate = m.DueDate.ToString("yyyy-MM-dd"),

                Status = m.MilestoneStatus,

                StoryPoints = m.StoryPoints,

                IsOverdue = m.MilestoneStatus != MilestoneStatusConstants.Done && m.DueDate < today

            }).ToList(),

            Allocations = allocations,

            RecentLoggedHours = timesheetHours

        };

    }



    public async Task<AiSkillMatchContextModel?> GetSkillMatchContextAsync(long projectId, CancellationToken cancellationToken = default)

    {

        var project = await context.Projects.FindAsync([projectId], cancellationToken);

        if (project is null)

            return null;



        return new AiSkillMatchContextModel

        {

            Project = new AiProjectSummaryContext

            {

                ProjectId = project.Id,

                ProjectName = project.ProjectName,

                Description = project.Description

            },

            Candidates = []

        };

    }



    public Task<AiOrganizationalSkillMatchContextModel> GetOrganizationalSkillMatchContextAsync(

        CancellationToken cancellationToken = default) =>

        Task.FromResult(new AiOrganizationalSkillMatchContextModel { Candidates = [] });



    public async Task<AiSkillMatchRawData> GetSkillMatchRawDataAsync(CancellationToken cancellationToken = default)

    {

        var employees = await context.Users

            .Where(u => u.IsActive)

            .Join(context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })

            .Join(context.Roles, combined => combined.ur.RoleId, r => r.Id, (combined, r) => new { combined.u, r.RoleName })

            .Where(x => x.RoleName == RoleConstants.Employee)

            .Select(x => x.u)

            .Distinct()

            .ToListAsync(cancellationToken);



        var employeeIds = employees.Select(e => e.Id).ToList();



        var userSkills = await context.UserSkills

            .Where(us => employeeIds.Contains(us.UserId))

            .Join(context.Skills, us => us.SkillId, s => s.Id, (us, s) => new AiUserSkillRaw

            {

                UserId = us.UserId,

                SkillName = s.SkillName,

                Category = s.Category,

                ProficiencyLevel = us.ProficiencyLevel

            })

            .ToListAsync(cancellationToken);



        var resourceProfiles = await context.ResourceProfiles

            .Where(rp => employeeIds.Contains(rp.UserId))

            .ToListAsync(cancellationToken);



        var profileIds = resourceProfiles.Select(rp => rp.Id).ToList();



        var activeAllocations = profileIds.Count == 0

            ? []

            : await context.ProjectAllocations

                .Where(a => profileIds.Contains(a.ResourceProfileId) && a.AllocationStatus == AllocationStatusConstants.Active)

                .Join(context.Projects, a => a.ProjectId, p => p.Id, (a, p) => new AiActiveAllocationRaw

                {

                    ProjectId = a.ProjectId,

                    ResourceProfileId = a.ResourceProfileId,

                    AllocationPercentage = a.AllocationPercentage,

                    AllocationStartDate = a.AllocationStartDate,

                    AllocationEndDate = a.AllocationEndDate,

                    ProjectName = p.ProjectName

                })

                .ToListAsync(cancellationToken);



        return new AiSkillMatchRawData

        {

            Employees = employees,

            UserSkills = userSkills,

            ResourceProfiles = resourceProfiles,

            ActiveAllocations = activeAllocations

        };

    }

}


