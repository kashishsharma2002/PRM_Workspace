using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Audit;
using Server.Common.Timesheets;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Timesheets;
using Server.Models.Entities;
using Server.Services.Shared;

namespace Server.Services.Timesheets;

public partial class TimesheetService(
    IDbTransactionManager transactionManager,
    ITimesheetRepository timesheetRepository,
    IAllocationRepository allocationRepository,
    IProjectRepository projectRepository,
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IActivityTagRepository activityTagRepository,
    ISystemConfigRepository systemConfigRepository,
    IAuditService auditService,
    IMemoryCache memoryCache,
    ILogger<TimesheetService> logger) : ITimesheetService
{
    public async Task<TimesheetSubmitResponseDto> SubmitTimesheetAsync(
        long employeeId,
        long actorUserId,
        TimesheetSubmitRequestDto request,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Timesheet submit requested: EmployeeId={EmployeeId}, Week={WeekStartDate}, LineItems={LineItemCount}",
            employeeId, request.WeekStartDate, request.LineItems.Count);

        if (WeekDateHelper.IsFutureWeek(request.WeekStartDate))
            throw new ValidationAppException("Cannot submit a timesheet for a future week.");

        if (await timesheetRepository.HasSubmittedForWeekAsync(employeeId, request.WeekStartDate, cancellationToken))
            throw new ConflictAppException(
                $"A timesheet for week {request.WeekStartDate:dd-MM-yyyy} has already been submitted.");

        var existingTimesheet = await timesheetRepository.GetByEmployeeAndWeekAsync(
            employeeId, request.WeekStartDate, cancellationToken);
        if (existingTimesheet is not null && existingTimesheet.Status != TimesheetConstants.StatusMissed)
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

        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        try
        {
            Timesheet timesheet;
            if (existingTimesheet is not null)
            {
                existingTimesheet.Status = TimesheetConstants.StatusSubmitted;
                existingTimesheet.TotalHours = totalHours;
                existingTimesheet.Remarks = request.Remarks;
                existingTimesheet.SubmittedAt = now;
                existingTimesheet.UpdatedAt = now;
                timesheet = existingTimesheet;
                await timesheetRepository.SaveChangesAsync(cancellationToken);

                await auditService.LogUpdateAsync(
                    actorUserId,
                    AuditEntityConstants.Timesheets,
                    timesheet.Id,
                    new { Status = TimesheetConstants.StatusMissed, TotalHours = 0m },
                    new
                    {
                        timesheet.WeekStartDate,
                        timesheet.TotalHours,
                        timesheet.Status,
                        LineItemCount = request.LineItems.Count
                    },
                    cancellationToken);
            }
            else
            {
                timesheet = new Timesheet
                {
                    ResourceProfileId = employeeId,
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

                await auditService.LogCreateAsync(
                    actorUserId,
                    AuditEntityConstants.Timesheets,
                    timesheet.Id,
                    new
                    {
                        timesheet.WeekStartDate,
                        timesheet.TotalHours,
                        LineItemCount = request.LineItems.Count
                    },
                    cancellationToken);
            }

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

            await timesheetRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Timesheet submitted. {EntityName} {EntityId} by {ActorUserId}",
                AuditEntityConstants.Timesheets, timesheet.Id, actorUserId);

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

        var hasSubmitted = await timesheetRepository.HasSubmittedForWeekAsync(
            employeeId, lastCompletedWeek, cancellationToken);
        return !hasSubmitted;
    }

    public async Task<int> MarkMissedTimesheetsAsync(CancellationToken cancellationToken = default)
    {
        var lastWeekStart = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var weekEnd = WeekDateHelper.GetWeekEnd(lastWeekStart);
        var allocations = await allocationRepository.GetAllActiveForWeekAsync(lastWeekStart, weekEnd, cancellationToken);
        var employeeIds = allocations.Select(a => a.ResourceProfileId).Distinct().ToList();

        if (employeeIds.Count == 0)
            return 0;

        var existingEmployeeIds = await timesheetRepository.GetEmployeeIdsWithTimesheetForWeekAsync(
            employeeIds, lastWeekStart, cancellationToken);
        var existingSet = existingEmployeeIds.ToHashSet();
        var now = DateTime.UtcNow;
        var created = 0;

        foreach (var employeeId in employeeIds)
        {
            if (existingSet.Contains(employeeId))
                continue;

            await timesheetRepository.AddAsync(new Timesheet
            {
                ResourceProfileId = employeeId,
                WeekStartDate = lastWeekStart,
                Status = TimesheetConstants.StatusMissed,
                TotalHours = 0,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);

            created++;
        }

        if (created > 0)
            await timesheetRepository.SaveChangesAsync(cancellationToken);

        return created;
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
            return TimesheetDefaults.DefaultMaxWeeklyHours;

        return maxHours;
    }
}
