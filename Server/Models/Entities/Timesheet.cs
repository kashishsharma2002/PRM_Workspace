using Server.Common;
using Server.Common.Allocations;

namespace Server.Models.Entities;

public class Timesheet
{
    public long Id { get; set; }
    public long ResourceProfileId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string Status { get; set; } = TimesheetConstants.StatusSubmitted;
    public decimal TotalHours { get; set; }
    public string? Remarks { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
