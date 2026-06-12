using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Employee;

public static class ViewTimesheetsScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        try
        {
            ConsoleHelper.PrintHeader("My Timesheets");
            var timesheets = await clients.Employee.GetMyTimesheetsAsync();
            if (timesheets is null || timesheets.Count == 0)
            {
                Console.WriteLine("No timesheets found.");
                ConsoleHelper.PrintDivider();
                Console.WriteLine("Press any key to go back...");
                Console.ReadKey(intercept: true);
                return;
            }

            Console.WriteLine($"{"ID",-8}{"Week Start",-14}{"Total Hrs",-12}{"Status"}");
            ConsoleHelper.PrintDivider();
            foreach (var item in timesheets)
            {
                var statusDisplay = item.Status == "MISSED" ? $"{item.Status}    ⚠" : item.Status;
                Console.WriteLine($"{item.Id,-8}{DateInputHelper.FormatDisplay(item.WeekStartDate),-14}{item.TotalHours,5:0.#} hrs    {statusDisplay}");
            }

            ConsoleHelper.PrintDivider();
            Console.Write("[V] View week details  [B] Back — choice: ");
            var choice = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (choice != "V")
                return;

            Console.Write("Enter timesheet ID: ");
            if (!long.TryParse(Console.ReadLine()?.Trim(), out var timesheetId))
            {
                ConsoleHelper.PrintError("Invalid timesheet ID.");
                return;
            }

            var detail = await clients.Employee.GetMyTimesheetDetailAsync(timesheetId);
            if (detail is null)
            {
                ConsoleHelper.PrintError("Timesheet not found.");
                return;
            }

            ConsoleHelper.PrintDivider();
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
