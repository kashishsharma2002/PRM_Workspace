using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Manager;

public static class TimesheetsScreen
{
    public static async Task RunAsync(RestClient client)
    {
        try
        {
            ConsoleHelper.PrintHeader("Timesheets — My Team");
            Console.WriteLine("Filter by week (DD-MM-YYYY) or press Enter for current week:");
            Console.Write("Week: ");
            var weekInput = Console.ReadLine();

            DateOnly weekStart;
            if (string.IsNullOrWhiteSpace(weekInput))
            {
                weekStart = DateInputHelper.GetLastMonday();
            }
            else if (!DateInputHelper.TryParseWeekStart(weekInput, out weekStart, out var dateError))
            {
                ConsoleHelper.PrintError(dateError ?? "Invalid week date.");
                return;
            }

            var response = await client.GetAsync<TeamTimesheetListResponse>(
                $"/api/timesheets/team?week={weekStart:yyyy-MM-dd}",
                requireAuth: true);

            if (response is null)
            {
                ConsoleHelper.PrintError("Failed to load team timesheets.");
                return;
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine($"Week: {DateInputHelper.FormatDisplay(response.WeekStartDate)}");
            Console.WriteLine($"{"ID",-8}{"Employee",-18}{"Project",-18}{"Hrs",-6}{"Status"}");
            ConsoleHelper.PrintDivider();

            if (response.Rows.Count == 0)
            {
                Console.WriteLine("No timesheet entries for this week.");
            }
            else
            {
                foreach (var row in response.Rows)
                {
                    var status = row.Status == "MISSED" ? $"{row.Status} !" : row.Status;
                    var idDisplay = row.TimesheetId?.ToString() ?? "-";
                    Console.WriteLine(
                        $"{idDisplay,-8}{row.EmployeeName,-18}{row.ProjectName,-18}{row.HoursLogged,4:0.#}   {status}");
                }
            }

            ConsoleHelper.PrintDivider();
            Console.Write("[V] View employee timesheet detail  [B] Back — choice: ");
            var choice = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (choice != "V")
                return;

            Console.Write("Enter timesheet ID: ");
            if (!long.TryParse(Console.ReadLine()?.Trim(), out var timesheetId))
            {
                ConsoleHelper.PrintError("Invalid timesheet ID.");
                return;
            }

            var detail = await client.GetAsync<ManagerTimesheetDetail>(
                $"/api/timesheets/{timesheetId}",
                requireAuth: true);

            if (detail is null)
            {
                ConsoleHelper.PrintError("Timesheet not found.");
                return;
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine($"Employee: {detail.EmployeeName}");
            Console.WriteLine($"Week: {DateInputHelper.FormatDisplay(detail.WeekStartDate)} — Status: {detail.Status}");
            Console.WriteLine($"{"Project",-18}{"Hrs",-8}{"Activity Tags"}");
            ConsoleHelper.PrintDivider();
            foreach (var line in detail.LineItems)
            {
                var tags = string.Join(", ", line.ActivityTags);
                Console.WriteLine($"{line.ProjectName,-18}{line.HoursLogged,5:0.#}    {tags}");
            }

            Console.WriteLine($"Total: {detail.TotalHours:0.#} hrs");
            ConsoleHelper.PrintDivider();
            Console.WriteLine("Press any key to go back...");
            Console.ReadKey(intercept: true);
        }
        catch (SessionExpiredException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorDisplayHelper.HandleException(ex);
        }
    }
}
