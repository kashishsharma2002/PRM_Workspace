namespace Client.HttpClients;

public class AdminClient(RestClient restClient) : IAdminClient
{
    public Task<UserListResponse?> GetUsersAsync() =>
        restClient.GetAsync<UserListResponse>("/api/users", requireAuth: true);

    public Task<CreateUserResponse?> CreateUserAsync(CreateUserRequest request) =>
        restClient.PostAsync<CreateUserResponse>("/api/users", request, requireAuth: true);

    public Task ResetPasswordAsync(long userId, ResetPasswordRequest request) =>
        restClient.PutAsync<object>($"/api/users/{userId}/reset-password", request, requireAuth: true);

    public Task DeactivateUserAsync(long userId) =>
        restClient.PutAsync<object>($"/api/users/{userId}/deactivate", new { }, requireAuth: true);

    public Task ReactivateUserAsync(long userId) =>
        restClient.PutAsync<object>($"/api/users/{userId}/reactivate", new { }, requireAuth: true);

    public Task<EmployeeListResponse?> GetEmployeesAsync(string? query = null)
    {
        var endpoint = string.IsNullOrWhiteSpace(query) ? "/api/employees" : $"/api/employees?{query}";
        return restClient.GetAsync<EmployeeListResponse>(endpoint, requireAuth: true);
    }

    public Task<EmployeeDetail?> GetEmployeeAsync(long employeeId) =>
        restClient.GetAsync<EmployeeDetail>($"/api/employees/{employeeId}", requireAuth: true);

    public Task UpdateEmployeeAsync(long employeeId, UpdateEmployeeRequest request) =>
        restClient.PutAsync<object>($"/api/employees/{employeeId}", request, requireAuth: true);

    public Task DeactivateEmployeeAsync(long employeeId) =>
        restClient.PutAsync<object>($"/api/employees/{employeeId}/deactivate", new { }, requireAuth: true);

    public Task AssignManagerAsync(long employeeId, AssignManagerRequest request) =>
        restClient.PutAsync<object>($"/api/employees/{employeeId}/assign-manager", request, requireAuth: true);

    public Task AddSkillAsync(long employeeId, AddSkillRequest request) =>
        restClient.PostAsync<object>($"/api/employees/{employeeId}/skills", request, requireAuth: true);

    public Task UpdateSkillProficiencyAsync(long employeeId, long skillId, UpdateSkillProficiencyRequest request) =>
        restClient.PutAsync<object>($"/api/employees/{employeeId}/skills/{skillId}", request, requireAuth: true);

    public Task DeleteSkillAsync(long employeeId, long skillId) =>
        restClient.DeleteAsync<object>($"/api/employees/{employeeId}/skills/{skillId}", requireAuth: true);

    public Task<ProjectListResponse?> GetProjectsAsync() =>
        restClient.GetAsync<ProjectListResponse>("/api/projects", requireAuth: true);

    public Task<ProjectDetail?> GetProjectAsync(long projectId) =>
        restClient.GetAsync<ProjectDetail>($"/api/projects/{projectId}", requireAuth: true);

    public Task<CreateProjectResponse?> CreateProjectAsync(CreateProjectRequest request) =>
        restClient.PostAsync<CreateProjectResponse>("/api/projects", request, requireAuth: true);

    public Task UpdateProjectAsync(long projectId, UpdateProjectRequest request) =>
        restClient.PutAsync<object>($"/api/projects/{projectId}", request, requireAuth: true);

    public Task<MilestoneListResponse?> GetMilestonesAsync(long projectId) =>
        restClient.GetAsync<MilestoneListResponse>($"/api/projects/{projectId}/milestones", requireAuth: true);

    public Task CreateMilestoneAsync(long projectId, CreateMilestoneRequest request) =>
        restClient.PostAsync<object>($"/api/projects/{projectId}/milestones", request, requireAuth: true);

    public Task UpdateMilestoneStatusAsync(long projectId, long milestoneId, UpdateMilestoneStatusRequest request) =>
        restClient.PutAsync<object>($"/api/projects/{projectId}/milestones/{milestoneId}/status", request, requireAuth: true);

    public Task<AllocationListResponse?> GetAllocationsAsync(string? query = null)
    {
        var endpoint = string.IsNullOrWhiteSpace(query) ? "/api/allocations" : $"/api/allocations?{query}";
        return restClient.GetAsync<AllocationListResponse>(endpoint, requireAuth: true);
    }

    public Task<SystemConfigResponse?> GetSystemConfigAsync() =>
        restClient.GetAsync<SystemConfigResponse>("/api/system-config", requireAuth: true);

    public Task UpdateSystemConfigAsync(UpdateSystemConfigRequest request) =>
        restClient.PutAsync<object>("/api/system-config", request, requireAuth: true);
}
