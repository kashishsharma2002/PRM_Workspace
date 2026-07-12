using Server.Models.DTOs.Employees;

namespace Server.Services.Employees;

public interface IEmployeeAdminService
{
    Task<EmployeeListResponseDto> GetAllEmployeesAsync(string? status, string? department, CancellationToken cancellationToken = default);
    Task<EmployeeDetailDto> GetEmployeeByIdAsync(long employeeId, CancellationToken cancellationToken = default);
    Task UpdateEmployeeAsync(long employeeId, UpdateEmployeeRequestDto request, CancellationToken cancellationToken = default);
    Task DeactivateEmployeeAsync(long actorUserId, long employeeId, CancellationToken cancellationToken = default);
    Task AddSkillAsync(long actorUserId, long employeeId, AddSkillRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateSkillProficiencyAsync(long actorUserId, long employeeId, long skillId, UpdateSkillProficiencyRequestDto request, CancellationToken cancellationToken = default);
    Task RemoveSkillAsync(long actorUserId, long employeeId, long skillId, CancellationToken cancellationToken = default);
    Task AssignManagerAsync(long actorUserId, long employeeId, AssignManagerRequestDto request, CancellationToken cancellationToken = default);
}
