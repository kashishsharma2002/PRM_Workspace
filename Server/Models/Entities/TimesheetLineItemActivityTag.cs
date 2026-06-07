namespace Server.Models.Entities;

public class TimesheetLineItemActivityTag
{
    public long TimesheetLineItemId { get; set; }
    public long ActivityTagId { get; set; }
    public string? CustomTagText { get; set; }
}
