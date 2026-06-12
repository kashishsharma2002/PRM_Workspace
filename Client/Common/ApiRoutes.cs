namespace Client.Common;

/// <summary>
/// Client API route definitions. Must match server controller route attributes.
/// Contract verification:
/// - TimesheetAllocations → Server: [HttpGet("week-allocations")] in TimesheetController
/// - EmployeeAssignManager → Server: [HttpPut("{id:long}/manager")] in EmployeeController
/// </summary>
public static class ApiRoutes
{
    public const string AuthLogin = "/api/auth/login";
    public const string AuthChangePassword = "/api/auth/change-password";

    public const string Users = "/api/users";
    public static string UserResetPassword(long userId) => $"/api/users/{userId}/reset-password";
    public static string UserDeactivate(long userId) => $"/api/users/{userId}/deactivate";
    public static string UserReactivate(long userId) => $"/api/users/{userId}/reactivate";

    public const string Employees = "/api/employees";
    public static string EmployeesWithQuery(string query) => $"/api/employees?{query}";
    public static string EmployeeById(long employeeId) => $"/api/employees/{employeeId}";
    public static string EmployeeDeactivate(long employeeId) => $"/api/employees/{employeeId}/deactivate";
    public static string EmployeeAssignManager(long employeeId) => $"/api/employees/{employeeId}/manager";
    public static string EmployeeSkills(long employeeId) => $"/api/employees/{employeeId}/skills";
    public static string EmployeeSkill(long employeeId, long skillId) => $"/api/employees/{employeeId}/skills/{skillId}";
    public const string EmployeesMyTeam = "/api/employees/my-team";
    public static string EmployeeTeamMember(long employeeId) => $"/api/employees/my-team/{employeeId}";

    public const string Projects = "/api/projects";
    public const string ProjectsMy = "/api/projects/my";
    public static string ProjectById(long projectId) => $"/api/projects/{projectId}";
    public static string ProjectManager(long projectId) => $"/api/projects/{projectId}/manager";
    public static string ProjectMilestones(long projectId) => $"/api/projects/{projectId}/milestones";
    public static string ProjectMilestoneStatus(long projectId, long milestoneId) =>
        $"/api/projects/{projectId}/milestones/{milestoneId}/status";

    public const string Allocations = "/api/allocations";
    public static string AllocationsWithQuery(string query) => $"/api/allocations?{query}";
    public const string AllocationsMy = "/api/allocations/my";
    public static string AllocationEnd(long allocationId) => $"/api/allocations/{allocationId}/end";

    public const string SystemConfig = "/api/system-config";

    public const string Timesheets = "/api/timesheets";
    public const string TimesheetsMy = "/api/timesheets/my";
    public static string TimesheetMyById(long timesheetId) => $"/api/timesheets/my/{timesheetId}";
    public const string TimesheetsTeam = "/api/timesheets/team";
    public static string TimesheetsTeamWithWeek(DateOnly weekStart) =>
        $"/api/timesheets/team?weekStart={weekStart:yyyy-MM-dd}";
    public static string TimesheetTeamById(long timesheetId) => $"/api/timesheets/team/{timesheetId}";
    public static string TimesheetAllocations(DateOnly weekStart) =>
        $"/api/timesheets/week-allocations?weekStart={weekStart:yyyy-MM-dd}";
    public const string TimesheetsReminder = "/api/timesheets/reminder";

    public const string ActivityTags = "/api/activity-tags";

    public static string AiSkillMatch(string requirement) =>
        $"/api/ai/skill-match?requirement={Uri.EscapeDataString(requirement)}";

    public static string AiProjectSkillMatch(long projectId, string requirement) =>
        $"/api/ai/projects/{projectId}/skill-match?requirement={Uri.EscapeDataString(requirement)}";

    public static string AiProjectRiskSummary(long projectId) =>
        $"/api/ai/projects/{projectId}/risk-summary";

    public static string AiTeamBuilder(string requirement) =>
        $"/api/ai/team-builder?requirement={Uri.EscapeDataString(requirement)}";
}
