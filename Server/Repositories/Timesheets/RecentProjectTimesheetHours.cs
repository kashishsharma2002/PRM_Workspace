namespace Server.Repositories.Timesheets;

public record RecentProjectTimesheetHours(
    string EmployeeName,
    decimal HoursLogged,
    DateOnly? WorkDate);
