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
using Server.Services.SystemConfig;

namespace Server.Services.Timesheets;

public partial class TimesheetService(
    IDbTransactionManager transactionManager,
    ITimesheetRepository timesheetRepository,
    IAllocationRepository allocationRepository,
    IProjectRepository projectRepository,
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IActivityTagRepository activityTagRepository,
    ISystemConfigService systemConfigService,
    IAuditService auditService,
    ISchedulerTimesheetService schedulerTimesheetService,
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

        var (existingTimesheet, activeAllocations, maxWeeklyHours, totalHours) =
            await ValidateSubmissionAsync(employeeId, request, cancellationToken);

        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        try
        {
            var timesheet = await PersistTimesheetAsync(
                employeeId,
                actorUserId,
                request,
                existingTimesheet,
                totalHours,
                cancellationToken);

            await PersistLineItemsAsync(timesheet, request, cancellationToken);

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

    private async Task<(Timesheet? ExistingTimesheet, IReadOnlyList<ProjectAllocation> ActiveAllocations, decimal MaxWeeklyHours, decimal TotalHours)>
        ValidateSubmissionAsync(
            long employeeId,
            TimesheetSubmitRequestDto request,
            CancellationToken cancellationToken)
    {
        if (WeekDateHelper.IsFutureWeek(request.WeekStartDate))
            throw new ValidationAppException("Cannot submit a timesheet for a future week.");

        var profile = await employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee profile not found.");
        if (profile.IsTimesheetFrozen)
            throw new ForbiddenAppException(
                "Your timesheet access is frozen. Contact your manager to restore submission privileges.");

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

        var maxWeeklyHours = await systemConfigService.GetMaxWeeklyHoursAsync(cancellationToken);
        var allocationByProject = activeAllocations.ToDictionary(a => a.ProjectId);
        var totalHours = request.LineItems.Sum(li => li.HoursLogged);

        if (totalHours > maxWeeklyHours)
            throw new ValidationAppException(
                $"Total hours {totalHours} exceed the maximum weekly limit of {maxWeeklyHours}.");

        var projectIds = request.LineItems.Select(li => li.ProjectId).Distinct().ToList();
        var projectsById = await projectRepository.GetByIdsAsync(projectIds, cancellationToken);

        foreach (var lineItem in request.LineItems)
        {
            if (!allocationByProject.TryGetValue(lineItem.ProjectId, out var allocation))
            {
                var projectName = projectsById.TryGetValue(lineItem.ProjectId, out var project)
                    ? project.ProjectName
                    : $"Project {lineItem.ProjectId}";
                throw new ValidationAppException(
                    $"You are not allocated to {projectName} for the selected week.");
            }

            var maxProjectHours = allocation.AllocationPercentage / 100m * maxWeeklyHours;
            if (lineItem.HoursLogged > maxProjectHours)
            {
                var projectName = projectsById.TryGetValue(lineItem.ProjectId, out var project)
                    ? project.ProjectName
                    : $"Project {lineItem.ProjectId}";
                throw new ValidationAppException(
                    $"{projectName}: {lineItem.HoursLogged} hours exceeds allocation cap of {maxProjectHours:0.##}.");
            }

            await ValidateActivityTagsAsync(lineItem, cancellationToken);
        }

        return (existingTimesheet, activeAllocations, maxWeeklyHours, totalHours);
    }

    private async Task<Timesheet> PersistTimesheetAsync(
        long employeeId,
        long actorUserId,
        TimesheetSubmitRequestDto request,
        Timesheet? existingTimesheet,
        decimal totalHours,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var profile = await employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee profile not found.");
        var employeeUser = await userRepository.GetByIdAsync(profile.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Employee user not found.");
        var weekLabel = $"Week of {request.WeekStartDate:dd-MM-yyyy}";

        if (existingTimesheet is not null)
        {
            existingTimesheet.Status = TimesheetConstants.StatusSubmitted;
            existingTimesheet.TotalHours = totalHours;
            existingTimesheet.Remarks = request.Remarks;
            existingTimesheet.SubmittedAt = now;
            existingTimesheet.UpdatedAt = now;
            await timesheetRepository.SaveChangesAsync(cancellationToken);

            await auditService.LogUpdateAsync(
                actorUserId,
                AuditEntityConstants.Timesheets,
                existingTimesheet.Id,
                new { Status = TimesheetConstants.StatusMissed, TotalHours = 0m },
                new
                {
                    existingTimesheet.WeekStartDate,
                    existingTimesheet.TotalHours,
                    existingTimesheet.Status,
                    LineItemCount = request.LineItems.Count
                },
                cancellationToken,
                AuditMessageBuilder.BuildTimesheetActionSummary("Submitted", employeeUser.FullName, weekLabel));

            return existingTimesheet;
        }

        var timesheet = new Timesheet
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
            cancellationToken,
            AuditMessageBuilder.BuildTimesheetActionSummary("Submitted", employeeUser.FullName, weekLabel));

        return timesheet;
    }

    private async Task PersistLineItemsAsync(
        Timesheet timesheet,
        TimesheetSubmitRequestDto request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

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
        var projectIds = lineItems.Select(li => li.ProjectId).Distinct().ToList();
        var projectsById = await projectRepository.GetByIdsAsync(projectIds, cancellationToken);

        var detailLineItems = new List<TimesheetDetailLineItemDto>();
        foreach (var lineItem in lineItems)
        {
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
                ProjectName = projectsById.TryGetValue(lineItem.ProjectId, out var project)
                    ? project.ProjectName
                    : "Unknown",
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
        var maxWeeklyHours = await systemConfigService.GetMaxWeeklyHoursAsync(cancellationToken);
        var projectIds = allocations.Select(a => a.ProjectId).Distinct().ToList();
        var projectsById = await projectRepository.GetByIdsAsync(projectIds, cancellationToken);

        return allocations.Select(allocation => new EmployeeWeekAllocationDto
        {
            ProjectId = allocation.ProjectId,
            ProjectName = projectsById.TryGetValue(allocation.ProjectId, out var project)
                ? project.ProjectName
                : "Unknown",
            AllocationPercentage = allocation.AllocationPercentage,
            MaxHours = allocation.AllocationPercentage / 100m * maxWeeklyHours
        }).ToList();
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

    public async Task<TimesheetReminderResponseDto> GetTimesheetReminderAsync(
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var weekStart = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var profile = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        var showReminder = await HasMissedTimesheetReminderAsync(employeeId, cancellationToken);

        return new TimesheetReminderResponseDto
        {
            ShowReminder = showReminder,
            WeekStartDate = weekStart,
            IsTimesheetFrozen = profile?.IsTimesheetFrozen ?? false
        };
    }

    public Task<int> MarkMissedTimesheetsAsync(CancellationToken cancellationToken = default) =>
        schedulerTimesheetService.MarkMissedTimesheetsAsync(cancellationToken);

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
}
