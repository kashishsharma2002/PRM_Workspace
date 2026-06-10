using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Server.Common;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Timesheets;
using Server.Models.Entities;
using Server.Repositories.Interfaces;
using Server.Services.Interfaces;

namespace Server.Services;

public class TimesheetService(
    PrmDbContext context,
    ITimesheetRepository timesheetRepository,
    IAllocationRepository allocationRepository,
    IProjectRepository projectRepository,
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IActivityTagRepository activityTagRepository,
    ISystemConfigRepository systemConfigRepository,
    IAuditLogRepository auditLogRepository,
    IMemoryCache memoryCache) : ITimesheetService
{
    public async Task<TimesheetSubmitResponseDto> SubmitTimesheetAsync(
        long employeeId,
        long actorUserId,
        TimesheetSubmitRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (WeekDateHelper.IsFutureWeek(request.WeekStartDate))
            throw new ValidationAppException("Cannot submit a timesheet for a future week.");

        if (await timesheetRepository.ExistsForWeekAsync(employeeId, request.WeekStartDate, cancellationToken))
            throw new ConflictAppException(
                $"A timesheet for week {request.WeekStartDate:dd-MM-yyyy} has already been submitted.");

        var weekEnd = WeekDateHelper.GetWeekEnd(request.WeekStartDate);
        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdForWeekAsync(
            employeeId, request.WeekStartDate, weekEnd, cancellationToken);

        if (activeAllocations.Count == 0)
            throw new ValidationAppException("You have no active allocations for the selected week.");

        var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);
        var allocationByProject = activeAllocations.ToDictionary(a => a.ProjectId);
        var totalHours = request.LineItems.Sum(li => li.HoursLogged);

        if (totalHours > maxWeeklyHours)
            throw new ValidationAppException(
                $"Total hours {totalHours} exceed the maximum weekly limit of {maxWeeklyHours}.");

        foreach (var lineItem in request.LineItems)
        {
            if (!allocationByProject.TryGetValue(lineItem.ProjectId, out var allocation))
            {
                var project = await projectRepository.GetByIdAsync(lineItem.ProjectId, cancellationToken);
                var projectName = project?.ProjectName ?? $"Project {lineItem.ProjectId}";
                throw new ValidationAppException(
                    $"You are not allocated to {projectName} for the selected week.");
            }

            var maxProjectHours = allocation.AllocationPercentage / 100m * maxWeeklyHours;
            if (lineItem.HoursLogged > maxProjectHours)
            {
                var project = await projectRepository.GetByIdAsync(lineItem.ProjectId, cancellationToken);
                var projectName = project?.ProjectName ?? $"Project {lineItem.ProjectId}";
                throw new ValidationAppException(
                    $"{projectName}: {lineItem.HoursLogged} hours exceeds allocation cap of {maxProjectHours:0.##}.");
            }

            await ValidateActivityTagsAsync(lineItem, cancellationToken);
        }

        var now = DateTime.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var timesheet = new Timesheet
            {
                EmployeeId = employeeId,
                WeekStartDate = request.WeekStartDate,
                Status = TimesheetConstants.StatusSubmitted,
                TotalHours = totalHours,
                Remarks = request.Remarks,
                SubmittedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            await timesheetRepository.AddAsync(timesheet, cancellationToken);
            await timesheetRepository.SaveChangesAsync(cancellationToken);

            foreach (var lineItem in request.LineItems)
            {
                var entity = new TimesheetLineItem
                {
                    TimesheetId = timesheet.Id,
                    ProjectId = lineItem.ProjectId,
                    HoursLogged = lineItem.HoursLogged,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await timesheetRepository.AddLineItemAsync(entity, cancellationToken);
                await timesheetRepository.SaveChangesAsync(cancellationToken);

                var tags = await activityTagRepository.GetByIdsAsync(lineItem.ActivityTagIds, cancellationToken);
                foreach (var tag in tags)
                {
                    await timesheetRepository.AddLineItemTagAsync(new TimesheetLineItemActivityTag
                    {
                        TimesheetLineItemId = entity.Id,
                        ActivityTagId = tag.Id,
                        CustomTagText = tag.TagCode == TimesheetConstants.OtherTagCode
                            ? lineItem.CustomTagText?.Trim()
                            : null
                    }, cancellationToken);
                }
            }

            await auditLogRepository.AddAsync(new AuditLog
            {
                ActorUserId = actorUserId,
                EntityName = "TIMESHEETS",
                EntityId = timesheet.Id,
                ActionType = "CREATE",
                NewValues = JsonSerializer.Serialize(new
                {
                    timesheet.WeekStartDate,
                    timesheet.TotalHours,
                    LineItemCount = request.LineItems.Count
                }),
                CreatedAt = now
            }, cancellationToken);

            await timesheetRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new TimesheetSubmitResponseDto
            {
                TimesheetId = timesheet.Id,
                WeekStartDate = timesheet.WeekStartDate,
                Status = timesheet.Status,
                TotalHours = timesheet.TotalHours
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<TimesheetHistoryItemDto>> GetMyTimesheetsAsync(
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var timesheets = await timesheetRepository.GetByEmployeeAsync(employeeId, cancellationToken);
        return timesheets.Select(t => new TimesheetHistoryItemDto
        {
            Id = t.Id,
            WeekStartDate = t.WeekStartDate,
            TotalHours = t.TotalHours,
            Status = t.Status
        }).ToList();
    }

    public async Task<TimesheetDetailDto> GetTimesheetDetailAsync(
        long employeeId,
        long timesheetId,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await timesheetRepository.GetByIdForEmployeeAsync(timesheetId, employeeId, cancellationToken)
            ?? throw new NotFoundAppException("Timesheet not found.");

        var lineItems = await timesheetRepository.GetLineItemsByTimesheetIdAsync(timesheetId, cancellationToken);
        var lineItemIds = lineItems.Select(li => li.Id).ToList();
        var tagLinks = await timesheetRepository.GetTagsByLineItemIdsAsync(lineItemIds, cancellationToken);
        var tagIds = tagLinks.Select(t => t.ActivityTagId).Distinct().ToList();
        var tags = await activityTagRepository.GetByIdsAsync(tagIds, cancellationToken);
        var tagLookup = tags.ToDictionary(t => t.Id);

        var detailLineItems = new List<TimesheetDetailLineItemDto>();
        foreach (var lineItem in lineItems)
        {
            var project = await projectRepository.GetByIdAsync(lineItem.ProjectId, cancellationToken);
            var itemTags = tagLinks
                .Where(t => t.TimesheetLineItemId == lineItem.Id)
                .Select(t =>
                {
                    if (!tagLookup.TryGetValue(t.ActivityTagId, out var tag))
                        return string.Empty;

                    if (tag.TagCode == TimesheetConstants.OtherTagCode && !string.IsNullOrWhiteSpace(t.CustomTagText))
                        return t.CustomTagText;

                    return tag.TagName;
                })
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            detailLineItems.Add(new TimesheetDetailLineItemDto
            {
                ProjectName = project?.ProjectName ?? "Unknown",
                HoursLogged = lineItem.HoursLogged,
                ActivityTags = itemTags
            });
        }

        return new TimesheetDetailDto
        {
            Id = timesheet.Id,
            WeekStartDate = timesheet.WeekStartDate,
            Status = timesheet.Status,
            TotalHours = timesheet.TotalHours,
            LineItems = detailLineItems
        };
    }

    public async Task<IReadOnlyList<EmployeeWeekAllocationDto>> GetWeekAllocationsAsync(
        long employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var weekEnd = WeekDateHelper.GetWeekEnd(weekStart);
        var allocations = await allocationRepository.GetActiveByEmployeeIdForWeekAsync(
            employeeId, weekStart, weekEnd, cancellationToken);
        var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);
        var result = new List<EmployeeWeekAllocationDto>();

        foreach (var allocation in allocations)
        {
            var project = await projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken);
            result.Add(new EmployeeWeekAllocationDto
            {
                ProjectId = allocation.ProjectId,
                ProjectName = project?.ProjectName ?? "Unknown",
                AllocationPercentage = allocation.AllocationPercentage,
                MaxHours = allocation.AllocationPercentage / 100m * maxWeeklyHours
            });
        }

        return result;
    }

    public Task<IReadOnlyList<ActivityTagDto>> GetActivityTagsAsync(CancellationToken cancellationToken = default)
    {
        return memoryCache.GetOrCreateAsync(TimesheetConstants.ActivityTagsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(TimesheetConstants.ActivityTagsCacheMinutes);
            var tags = await activityTagRepository.GetAllActiveAsync(cancellationToken);
            return (IReadOnlyList<ActivityTagDto>)tags.Select(t => new ActivityTagDto
            {
                Id = t.Id,
                TagCode = t.TagCode,
                TagName = t.TagName,
                TagCategory = t.TagCategory
            }).ToList();
        })!;
    }

    public async Task<bool> HasMissedTimesheetReminderAsync(long employeeId, CancellationToken cancellationToken = default)
    {
        var lastCompletedWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var weekEnd = WeekDateHelper.GetWeekEnd(lastCompletedWeek);
        var allocations = await allocationRepository.GetActiveByEmployeeIdForWeekAsync(
            employeeId, lastCompletedWeek, weekEnd, cancellationToken);

        if (allocations.Count == 0)
            return false;

        var exists = await timesheetRepository.ExistsForWeekAsync(employeeId, lastCompletedWeek, cancellationToken);
        return !exists;
    }

    public Task MarkMissedTimesheetsAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public async Task<TeamTimesheetListResponseDto> GetTeamTimesheetsAsync(
        long managerUserId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default)
    {
        var resolvedWeek = weekStart ?? WeekDateHelper.GetCurrentWeekMonday();
        var weekEnd = WeekDateHelper.GetWeekEnd(resolvedWeek);
        var team = await employeeRepository.GetByManagerIdAsync(managerUserId, cancellationToken);
        var employeeIds = team.Select(e => e.Id).ToList();

        if (employeeIds.Count == 0)
        {
            return new TeamTimesheetListResponseDto
            {
                WeekStartDate = resolvedWeek,
                Rows = []
            };
        }

        var userIds = team.Select(e => e.UserId).Distinct().ToList();
        var users = await userRepository.GetByIdsAsync(userIds, cancellationToken);
        var employeeNameLookup = team.ToDictionary(
            e => e.Id,
            e => users.TryGetValue(e.UserId, out var user) ? user.FullName : "Unknown");

        var allocations = await allocationRepository.GetActiveByEmployeeIdsForWeekAsync(
            employeeIds, resolvedWeek, weekEnd, cancellationToken);
        var timesheets = await timesheetRepository.GetByEmployeeIdsAndWeekAsync(
            employeeIds, resolvedWeek, cancellationToken);
        var timesheetByEmployee = timesheets.ToDictionary(t => t.EmployeeId);

        var rows = new List<TeamTimesheetRowDto>();
        foreach (var allocation in allocations)
        {
            var project = await projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken);
            var projectName = project?.ProjectName ?? "Unknown";
            var employeeName = employeeNameLookup.GetValueOrDefault(allocation.EmployeeId, "Unknown");

            if (!timesheetByEmployee.TryGetValue(allocation.EmployeeId, out var timesheet))
                continue;

            if (timesheet.Status == TimesheetConstants.StatusMissed)
            {
                rows.Add(new TeamTimesheetRowDto
                {
                    TimesheetId = timesheet.Id,
                    EmployeeName = employeeName,
                    ProjectName = projectName,
                    HoursLogged = 0,
                    Status = TimesheetConstants.StatusMissed
                });
                continue;
            }

            if (timesheet.Status != TimesheetConstants.StatusSubmitted)
                continue;

            var lineItems = await timesheetRepository.GetLineItemsByTimesheetIdAsync(timesheet.Id, cancellationToken);
            var projectLine = lineItems.FirstOrDefault(li => li.ProjectId == allocation.ProjectId);
            if (projectLine is null)
                continue;

            rows.Add(new TeamTimesheetRowDto
            {
                TimesheetId = timesheet.Id,
                EmployeeName = employeeName,
                ProjectName = projectName,
                HoursLogged = projectLine.HoursLogged,
                Status = TimesheetConstants.StatusSubmitted
            });
        }

        return new TeamTimesheetListResponseDto
        {
            WeekStartDate = resolvedWeek,
            Rows = rows
        };
    }

    public async Task<ManagerTimesheetDetailDto> GetTimesheetForManagerAsync(
        long managerUserId,
        long timesheetId,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await timesheetRepository.GetByIdForEmployeeCheckAsync(timesheetId, cancellationToken)
            ?? throw new NotFoundAppException("Timesheet not found.");

        var employee = await employeeRepository.GetByIdAsync(timesheet.EmployeeId, cancellationToken)
            ?? throw new NotFoundAppException("Timesheet not found.");

        if (employee.ManagerId != managerUserId)
            throw new NotFoundAppException("Timesheet not found.");

        var user = await userRepository.GetByIdAsync(employee.UserId, cancellationToken);
        var detail = await GetTimesheetDetailAsync(timesheet.EmployeeId, timesheetId, cancellationToken);

        return new ManagerTimesheetDetailDto
        {
            Id = detail.Id,
            EmployeeName = user?.FullName ?? "Unknown",
            WeekStartDate = detail.WeekStartDate,
            Status = detail.Status,
            TotalHours = detail.TotalHours,
            LineItems = detail.LineItems
        };
    }

    private async Task ValidateActivityTagsAsync(
        TimesheetLineItemRequestDto lineItem,
        CancellationToken cancellationToken)
    {
        var tags = await activityTagRepository.GetByIdsAsync(lineItem.ActivityTagIds, cancellationToken);
        if (tags.Count != lineItem.ActivityTagIds.Distinct().Count())
            throw new ValidationAppException("One or more activity tags are invalid.");

        if (tags.Any(t => t.TagCode == TimesheetConstants.OtherTagCode)
            && string.IsNullOrWhiteSpace(lineItem.CustomTagText))
        {
            throw new ValidationAppException("Custom tag text is required when selecting Other.");
        }
    }

    private async Task<decimal> GetMaxWeeklyHoursAsync(CancellationToken cancellationToken)
    {
        var config = await systemConfigRepository.GetByKeyAsync(ConfigKeys.MaxWeeklyHours, cancellationToken);
        if (config is null || !decimal.TryParse(config.ConfigValue, out var maxHours))
            return 40m;

        return maxHours;
    }
}
