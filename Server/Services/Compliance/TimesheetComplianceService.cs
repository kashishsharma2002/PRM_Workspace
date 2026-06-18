using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Audit;
using Server.Common.Emails;
using Server.Repositories.Allocations;
using Server.Repositories.Emails;
using Server.Repositories.Employees;
using Server.Repositories.SystemConfig;
using Server.Repositories.Timesheets;
using Server.Repositories.Users;
using Server.Services.Emails;
using Server.Services.Shared;

namespace Server.Services.Compliance;

public class TimesheetComplianceService(
    ISystemConfigRepository systemConfigRepository,
    IAllocationRepository allocationRepository,
    ITimesheetRepository timesheetRepository,
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IEmailLogRepository emailLogRepository,
    IEmailService emailService,
    IAuditService auditService,
    ILogger<TimesheetComplianceService> logger) : ITimesheetComplianceService
{
    public async Task ProcessTimesheetComplianceAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday(today);
        var weekEnd = WeekDateHelper.GetWeekEnd(lastWeek);
        var weekEndLabel = weekEnd.ToString("yyyy-MM-dd");
        var entityReference = $"WeekEnding:{weekEndLabel}";

        var deadlineOffset = await GetDeadlineWorkingDaysAfterWeekEndAsync(cancellationToken);
        var schedule = TimesheetComplianceCalendar.Build(lastWeek, deadlineOffset);
        var emailType = ResolveEmailType(today, schedule);
        if (emailType is null)
            return;

        var allocations = await allocationRepository.GetAllActiveForWeekAsync(lastWeek, weekEnd, cancellationToken);
        var employeeIds = allocations.Select(a => a.ResourceProfileId).Distinct().ToList();
        if (employeeIds.Count == 0)
            return;

        var nonCompliantIds = new List<long>();
        foreach (var employeeId in employeeIds)
        {
            if (!await timesheetRepository.HasSubmittedForWeekAsync(employeeId, lastWeek, cancellationToken))
                nonCompliantIds.Add(employeeId);
        }

        foreach (var employeeId in nonCompliantIds)
        {
            var profile = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (profile is null)
                continue;

            var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
                continue;

            var managerName = await ResolveManagerNameAsync(profile.ManagerId, cancellationToken);
            var placeholders = new Dictionary<string, string>
            {
                ["EmployeeName"] = user.FullName,
                ["WeekEndDate"] = weekEndLabel,
                ["ManagerName"] = managerName
            };

            if (emailType == EmailTypeConstants.TimesheetFreeze)
            {
                await FreezeEmployeeAsync(profile, user.FullName, cancellationToken);
                await TrySendEmployeeEmailAsync(
                    user.Email, emailType, placeholders, entityReference, employeeId, weekEndLabel, cancellationToken);
                await NotifyManagerOfFreezeAsync(profile, user.FullName, weekEndLabel, entityReference, cancellationToken);
                continue;
            }

            var alreadySent = await emailLogRepository.WasSentForReferenceAsync(
                user.Email, emailType, entityReference, cancellationToken);
            if (alreadySent)
                continue;

            await emailService.SendNotificationAsync(
                user.Email,
                emailType,
                placeholders,
                entityReference,
                $"compliance-{employeeId}-{weekEndLabel}-{emailType}",
                cancellationToken);
        }

        logger.LogInformation(
            "Timesheet compliance processed. EmailType={EmailType}, NonCompliantCount={Count}, Schedule={Schedule}",
            emailType,
            nonCompliantIds.Count,
            schedule);
    }

    private async Task TrySendEmployeeEmailAsync(
        string email,
        string emailType,
        Dictionary<string, string> placeholders,
        string entityReference,
        long employeeId,
        string weekEndLabel,
        CancellationToken cancellationToken)
    {
        var alreadySent = await emailLogRepository.WasSentForReferenceAsync(
            email, emailType, entityReference, cancellationToken);
        if (alreadySent)
            return;

        await emailService.SendNotificationAsync(
            email,
            emailType,
            placeholders,
            entityReference,
            $"compliance-{employeeId}-{weekEndLabel}-{emailType}",
            cancellationToken);
    }

    private static string? ResolveEmailType(DateOnly today, TimesheetComplianceSchedule schedule)
    {
        if (today == schedule.Reminder1)
            return EmailTypeConstants.TimesheetReminder1;
        if (today == schedule.Reminder2)
            return EmailTypeConstants.TimesheetReminder2;
        if (today == schedule.Freeze)
            return EmailTypeConstants.TimesheetFreeze;

        return null;
    }

    private async Task<int> GetDeadlineWorkingDaysAfterWeekEndAsync(CancellationToken cancellationToken)
    {
        var config = await systemConfigRepository.GetByKeyAsync(ConfigKeys.TimesheetDeadlineDay, cancellationToken);
        if (config is null || !int.TryParse(config.ConfigValue, out var days) || days < 0)
            return EmailDefaults.TimesheetDeadlineWorkingDaysAfterWeekEnd;

        return days;
    }

    private async Task<string> ResolveManagerNameAsync(long? managerUserId, CancellationToken cancellationToken)
    {
        if (managerUserId is null)
            return "your manager";

        var manager = await userRepository.GetByIdAsync(managerUserId.Value, cancellationToken);
        return manager?.FullName ?? "your manager";
    }

    private async Task NotifyManagerOfFreezeAsync(
        Models.Entities.ResourceProfile profile,
        string employeeName,
        string weekEndLabel,
        string entityReference,
        CancellationToken cancellationToken)
    {
        if (profile.ManagerId is null)
            return;

        var manager = await userRepository.GetByIdAsync(profile.ManagerId.Value, cancellationToken);
        if (manager is null || string.IsNullOrWhiteSpace(manager.Email))
            return;

        var managerEntityRef = $"{entityReference}:Manager";
        var alreadySent = await emailLogRepository.WasSentForReferenceAsync(
            manager.Email,
            EmailTypeConstants.TimesheetFreezeManager,
            managerEntityRef,
            cancellationToken);
        if (alreadySent)
            return;

        var placeholders = new Dictionary<string, string>
        {
            ["ManagerName"] = manager.FullName,
            ["EmployeeName"] = employeeName,
            ["WeekEndDate"] = weekEndLabel
        };

        await emailService.SendNotificationAsync(
            manager.Email,
            EmailTypeConstants.TimesheetFreezeManager,
            placeholders,
            managerEntityRef,
            $"compliance-mgr-{profile.Id}-{weekEndLabel}",
            cancellationToken);
    }

    private async Task FreezeEmployeeAsync(
        Models.Entities.ResourceProfile profile,
        string employeeName,
        CancellationToken cancellationToken)
    {
        if (profile.IsTimesheetFrozen)
            return;

        profile.IsTimesheetFrozen = true;
        profile.UpdatedAt = DateTime.UtcNow;
        await employeeRepository.UpdateAsync(profile, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);

        await auditService.LogUpdateAsync(
            profile.UserId,
            AuditEntityConstants.ResourceProfiles,
            profile.Id,
            new { is_timesheet_frozen = false },
            new { is_timesheet_frozen = true, employee = employeeName },
            cancellationToken);
    }
}
