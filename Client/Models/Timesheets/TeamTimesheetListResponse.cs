namespace Client.Models.Timesheets;

public class TeamTimesheetListResponse
{
    public DateOnly WeekStartDate { get; set; }
    public List<TeamTimesheetRow> Rows { get; set; } = [];
    public List<FrozenTeamMember> FrozenEmployees { get; set; } = [];
}
