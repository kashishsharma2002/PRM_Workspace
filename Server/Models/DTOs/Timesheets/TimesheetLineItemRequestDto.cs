namespace Server.Models.DTOs.Timesheets;

public class TimesheetLineItemRequestDto
{
    public long ProjectId { get; set; }
    public decimal HoursLogged { get; set; }
    public List<long> ActivityTagIds { get; set; } = [];
    public string? CustomTagText { get; set; }
}
