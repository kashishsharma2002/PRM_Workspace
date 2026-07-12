using Client.Common;

namespace Client.HttpClients;

public class AdminClient(RestClient restClient) : IAdminClient
{
    public Task<UserListResponse?> GetUsersAsync() =>
        restClient.GetAsync<UserListResponse>(ApiRoutes.Users, requireAuth: true);

    public Task<CreateUserResponse?> CreateUserAsync(CreateUserRequest request) =>
        restClient.PostAsync<CreateUserResponse>(ApiRoutes.Users, request, requireAuth: true);

    public Task ResetPasswordAsync(long userId, ResetPasswordRequest request) =>
        restClient.PutAsync<object>(ApiRoutes.UserResetPassword(userId), request, requireAuth: true);

    public Task UpdateUserRoleAsync(long userId, UpdateUserRoleRequest request) =>
        restClient.PutAsync<object>(ApiRoutes.UserRole(userId), request, requireAuth: true);

    public Task DeactivateUserAsync(long userId) =>
        restClient.PutAsync<object>(ApiRoutes.UserDeactivate(userId), new { }, requireAuth: true);

    public Task ReactivateUserAsync(long userId) =>
        restClient.PutAsync<object>(ApiRoutes.UserReactivate(userId), new { }, requireAuth: true);

    public Task<EmployeeListResponse?> GetEmployeesAsync(string? query = null)
    {
        var endpoint = string.IsNullOrWhiteSpace(query)
            ? ApiRoutes.Employees
            : ApiRoutes.EmployeesWithQuery(query);
        return restClient.GetAsync<EmployeeListResponse>(endpoint, requireAuth: true);
    }

    public Task<EmployeeDetail?> GetEmployeeAsync(long employeeId) =>
        restClient.GetAsync<EmployeeDetail>(ApiRoutes.EmployeeById(employeeId), requireAuth: true);

    public Task UpdateEmployeeAsync(long employeeId, UpdateEmployeeRequest request) =>
        restClient.PutAsync<object>(ApiRoutes.EmployeeById(employeeId), request, requireAuth: true);

    public Task DeactivateEmployeeAsync(long employeeId) =>
        restClient.PutAsync<object>(ApiRoutes.EmployeeDeactivate(employeeId), new { }, requireAuth: true);

    public Task AssignManagerAsync(long employeeId, AssignManagerRequest request) =>
        restClient.PutAsync<object>(ApiRoutes.EmployeeAssignManager(employeeId), request, requireAuth: true);

    public Task AddSkillAsync(long employeeId, AddSkillRequest request) =>
        restClient.PostAsync<object>(ApiRoutes.EmployeeSkills(employeeId), request, requireAuth: true);

    public Task UpdateSkillProficiencyAsync(long employeeId, long skillId, UpdateSkillProficiencyRequest request) =>
        restClient.PutAsync<object>(ApiRoutes.EmployeeSkill(employeeId, skillId), request, requireAuth: true);

    public Task DeleteSkillAsync(long employeeId, long skillId) =>
        restClient.DeleteAsync<object>(ApiRoutes.EmployeeSkill(employeeId, skillId), requireAuth: true);

    public Task<ProjectListResponse?> GetProjectsAsync() =>
        restClient.GetAsync<ProjectListResponse>(ApiRoutes.Projects, requireAuth: true);

    public Task<ProjectDetail?> GetProjectAsync(long projectId) =>
        restClient.GetAsync<ProjectDetail>(ApiRoutes.ProjectById(projectId), requireAuth: true);

    public Task<CreateProjectResponse?> CreateProjectAsync(CreateProjectRequest request) =>
        restClient.PostAsync<CreateProjectResponse>(ApiRoutes.Projects, request, requireAuth: true);

    public Task UpdateProjectAsync(long projectId, UpdateProjectRequest request) =>
        restClient.PutAsync<object>(ApiRoutes.ProjectById(projectId), request, requireAuth: true);

    public Task<MilestoneListResponse?> GetMilestonesAsync(long projectId) =>
        restClient.GetAsync<MilestoneListResponse>(ApiRoutes.ProjectMilestones(projectId), requireAuth: true);

    public Task CreateMilestoneAsync(long projectId, CreateMilestoneRequest request) =>
        restClient.PostAsync<object>(ApiRoutes.ProjectMilestones(projectId), request, requireAuth: true);

    public Task UpdateMilestoneStatusAsync(long projectId, long milestoneId, UpdateMilestoneStatusRequest request) =>
        restClient.PutAsync<object>(ApiRoutes.ProjectMilestoneStatus(projectId, milestoneId), request, requireAuth: true);

    public Task<AllocationListResponse?> GetAllocationsAsync(string? query = null)
    {
        var endpoint = string.IsNullOrWhiteSpace(query)
            ? ApiRoutes.Allocations
            : ApiRoutes.AllocationsWithQuery(query);
        return restClient.GetAsync<AllocationListResponse>(endpoint, requireAuth: true);
    }

    public Task<SystemConfigResponse?> GetSystemConfigAsync() =>
        restClient.GetAsync<SystemConfigResponse>(ApiRoutes.SystemConfig, requireAuth: true);

    public Task UpdateSystemConfigAsync(UpdateSystemConfigRequest request) =>
        restClient.PutAsync<object>(ApiRoutes.SystemConfig, request, requireAuth: true);

    public Task<RoleListResponse?> GetRolesAsync() =>
        restClient.GetAsync<RoleListResponse>(ApiRoutes.Roles, requireAuth: true);

    public Task<RolePermissionsResponse?> GetRolePermissionsAsync(string roleName) =>
        restClient.GetAsync<RolePermissionsResponse>(ApiRoutes.RolePermissions(roleName), requireAuth: true);

    public Task<AuditLogListResponse?> GetAuditLogsAsync(string? query = null)
    {
        var endpoint = string.IsNullOrWhiteSpace(query)
            ? ApiRoutes.AuditLogs
            : ApiRoutes.AuditLogsWithQuery(query);
        return restClient.GetAsync<AuditLogListResponse>(endpoint, requireAuth: true);
    }
}
