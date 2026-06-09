using Server.Models.DTOs.Employees;

namespace Server.Services.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeListResponseDto> GetAllEmployeesAsync(string? status, string? department, CancellationToken cancellationToken = default);
    Task<EmployeeDetailDto> GetEmployeeByIdAsync(long employeeId, CancellationToken cancellationToken = default);
    Task UpdateEmployeeAsync(long employeeId, UpdateEmployeeRequestDto request, CancellationToken cancellationToken = default);
    Task DeactivateEmployeeAsync(long actorUserId, long employeeId, CancellationToken cancellationToken = default);
    Task AddSkillAsync(long employeeId, AddSkillRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateSkillProficiencyAsync(long employeeId, long skillId, UpdateSkillProficiencyRequestDto request, CancellationToken cancellationToken = default);
    Task RemoveSkillAsync(long employeeId, long skillId, CancellationToken cancellationToken = default);
    Task AssignManagerAsync(long employeeId, AssignManagerRequestDto request, CancellationToken cancellationToken = default);
}
