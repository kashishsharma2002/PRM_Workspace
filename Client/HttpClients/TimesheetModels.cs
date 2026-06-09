namespace Client.HttpClients;

public class TimesheetSubmitRequest
{
    public DateOnly WeekStartDate { get; set; }
    public List<TimesheetLineItemRequest> LineItems { get; set; } = [];
    public string? Remarks { get; set; }
}

public class TimesheetLineItemRequest
{
    public long ProjectId { get; set; }
    public decimal HoursLogged { get; set; }
    public List<long> ActivityTagIds { get; set; } = [];
    public string? CustomTagText { get; set; }
}

public class TimesheetSubmitResponse
{
    public long TimesheetId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
}

public class TimesheetHistoryItem
{
    public long Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public decimal TotalHours { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TimesheetDetail
{
    public long Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public List<TimesheetDetailLineItem> LineItems { get; set; } = [];
}

public class TimesheetDetailLineItem
{
    public string ProjectName { get; set; } = string.Empty;
    public decimal HoursLogged { get; set; }
    public List<string> ActivityTags { get; set; } = [];
}

public class EmployeeWeekAllocation
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public decimal MaxHours { get; set; }
}

public class ActivityTagItem
{
    public long Id { get; set; }
    public string TagCode { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string? TagCategory { get; set; }
}

public class TimesheetReminderResponse
{
    public bool ShowReminder { get; set; }
    public DateOnly WeekStartDate { get; set; }
}

public class EmployeeAllocationItem
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationStartDate { get; set; }
    public DateOnly AllocationEndDate { get; set; }
    public string AllocationStatus { get; set; } = string.Empty;
}

public class EmployeeAllocationListResponse
{
    public List<EmployeeAllocationItem> Allocations { get; set; } = [];
    public decimal TotalUtilizationPercentage { get; set; }
}

public class TeamTimesheetRow
{
    public long? TimesheetId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal HoursLogged { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TeamTimesheetListResponse
{
    public DateOnly WeekStartDate { get; set; }
    public List<TeamTimesheetRow> Rows { get; set; } = [];
}

public class ManagerTimesheetDetail
{
    public long Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly WeekStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public List<TimesheetDetailLineItem> LineItems { get; set; } = [];
}
