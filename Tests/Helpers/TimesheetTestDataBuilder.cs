using Server.Models.DTOs.Timesheets;

namespace Tests.Helpers;

public class TimesheetTestDataBuilder
{
    private long _employeeId = 1;
    private DateOnly _weekStartDate = new(2026, 5, 12);
    private decimal _hoursPerProject = 20m;
    private long _projectId = 1;
    private List<long> _activityTagIds = [1];

    public TimesheetTestDataBuilder WithEmployeeId(long employeeId)
    {
        _employeeId = employeeId;
        return this;
    }

    public TimesheetTestDataBuilder WithWeekStartDate(DateOnly weekStartDate)
    {
        _weekStartDate = weekStartDate;
        return this;
    }

    public TimesheetTestDataBuilder WithHoursPerProject(decimal hours)
    {
        _hoursPerProject = hours;
        return this;
    }

    public TimesheetTestDataBuilder WithProjectId(long projectId)
    {
        _projectId = projectId;
        return this;
    }

    public TimesheetTestDataBuilder WithActivityTagIds(params long[] tagIds)
    {
        _activityTagIds = tagIds.ToList();
        return this;
    }

    public long EmployeeId => _employeeId;

    public DateOnly WeekStartDate => _weekStartDate;

    public TimesheetSubmitRequestDto BuildSubmitRequest() =>
        new()
        {
            WeekStartDate = _weekStartDate,
            LineItems =
            [
                new TimesheetLineItemRequestDto
                {
                    ProjectId = _projectId,
                    HoursLogged = _hoursPerProject,
                    ActivityTagIds = _activityTagIds
                }
            ]
        };
}
