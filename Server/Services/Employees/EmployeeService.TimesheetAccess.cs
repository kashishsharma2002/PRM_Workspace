using Server.Common;
using Server.Common.Audit;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Models.DTOs.Employees;

namespace Server.Services.Employees;

public partial class EmployeeService
{
    public async Task RestoreTimesheetAccessAsync(
        long managerUserId,
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var profile = await employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.", ErrorCodes.EmployeeNotFound);

        if (profile.ManagerId != managerUserId)
            throw new ForbiddenAppException("Employee is not on your team.", ErrorCodes.EmployeeNotOnTeam);

        if (!profile.IsTimesheetFrozen)
            throw new ValidationAppException("Timesheet access is not frozen for this employee.");

        var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken);
        var employeeName = user?.FullName ?? $"Employee {employeeId}";

        profile.IsTimesheetFrozen = false;
        profile.UpdatedAt = DateTime.UtcNow;
        await employeeRepository.UpdateAsync(profile, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);

        await auditService.LogUpdateAsync(
            managerUserId,
            AuditEntityConstants.ResourceProfiles,
            profile.Id,
            new { is_timesheet_frozen = true },
            new { is_timesheet_frozen = false, employee = employeeName, restored_by_manager = managerUserId },
            cancellationToken);

        logger.LogInformation(
            "Timesheet access restored for employee {EmployeeId} by manager {ManagerUserId}",
            employeeId,
            managerUserId);
    }
}
