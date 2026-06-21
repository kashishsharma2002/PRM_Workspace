namespace Client.HttpClients;

public interface IAdminClient
{
    Task<UserListResponse?> GetUsersAsync();
    Task<CreateUserResponse?> CreateUserAsync(CreateUserRequest request);
    Task ResetPasswordAsync(long userId, ResetPasswordRequest request);
    Task UpdateUserRoleAsync(long userId, UpdateUserRoleRequest request);
    Task DeactivateUserAsync(long userId);
    Task ReactivateUserAsync(long userId);
    Task<RoleListResponse?> GetRolesAsync();
    Task<RolePermissionsResponse?> GetRolePermissionsAsync(string roleName);
    Task<AuditLogListResponse?> GetAuditLogsAsync(string? query = null);
    Task<EmployeeListResponse?> GetEmployeesAsync(string? query = null);
    Task<EmployeeDetail?> GetEmployeeAsync(long employeeId);
    Task UpdateEmployeeAsync(long employeeId, UpdateEmployeeRequest request);
    Task DeactivateEmployeeAsync(long employeeId);
    Task AssignManagerAsync(long employeeId, AssignManagerRequest request);
    Task AddSkillAsync(long employeeId, AddSkillRequest request);
    Task UpdateSkillProficiencyAsync(long employeeId, long skillId, UpdateSkillProficiencyRequest request);
    Task DeleteSkillAsync(long employeeId, long skillId);
    Task<ProjectListResponse?> GetProjectsAsync();
    Task<ProjectDetail?> GetProjectAsync(long projectId);
    Task<CreateProjectResponse?> CreateProjectAsync(CreateProjectRequest request);
    Task UpdateProjectAsync(long projectId, UpdateProjectRequest request);
    Task<MilestoneListResponse?> GetMilestonesAsync(long projectId);
    Task CreateMilestoneAsync(long projectId, CreateMilestoneRequest request);
    Task UpdateMilestoneStatusAsync(long projectId, long milestoneId, UpdateMilestoneStatusRequest request);
    Task<AllocationListResponse?> GetAllocationsAsync(string? query = null);
    Task<SystemConfigResponse?> GetSystemConfigAsync();
    Task UpdateSystemConfigAsync(UpdateSystemConfigRequest request);
}
