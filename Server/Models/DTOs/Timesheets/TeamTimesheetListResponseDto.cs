namespace Server.Models.DTOs.Timesheets;

public class TeamTimesheetListResponseDto
{
    public DateOnly WeekStartDate { get; set; }
    public List<TeamTimesheetRowDto> Rows { get; set; } = [];
}
