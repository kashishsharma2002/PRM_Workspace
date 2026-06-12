namespace Client.Models.Timesheets;

public class TimesheetLineItemRequest
{
    public long ProjectId { get; set; }
    public decimal HoursLogged { get; set; }
    public List<long> ActivityTagIds { get; set; } = [];
    public string? CustomTagText { get; set; }
}
