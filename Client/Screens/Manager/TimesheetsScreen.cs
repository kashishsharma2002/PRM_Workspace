using Client.Common;
using Client.Helpers;
using Client.HttpClients;
using Client.Models.Timesheets;

namespace Client.Screens.Manager;

public static class TimesheetsScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(() => RunMenuAsync(clients));

    private static async Task RunMenuAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Timesheets — My Team");
            Console.WriteLine("1. View Team Timesheets");
            Console.WriteLine("2. Restore Frozen Timesheet Access");
            Console.WriteLine("0. Back");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case MenuChoices.One:
                    await ViewTeamTimesheetsAsync(clients);
                    break;
                case MenuChoices.Two:
                    await RestoreFrozenTimesheetsAsync(clients);
                    break;
                case MenuChoices.Exit:
                    return;
                default:
                    ConsoleHelper.PrintError("Invalid option.");
                    break;
            }
        }
    }

    private static async Task ViewTeamTimesheetsAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("View Team Timesheets");
            Console.WriteLine("Enter any date in the week (DD-MM-YYYY), or press Enter for the most recent completed week.");
            Console.WriteLine("Timesheets are grouped by the Monday that starts each week.");
            Console.Write("Week date: ");
            var weekInput = Console.ReadLine();

            if (!DateInputHelper.TryParseWeekStart(weekInput, out var weekStart, out var dateError))
            {
                ConsoleHelper.PrintError(dateError ?? "Invalid week date.");
                continue;
            }

            var response = await clients.Manager.GetTeamTimesheetsAsync(weekStart);

            if (!ApiLoadHelper.RequireLoaded(response, "Failed to load team timesheets."))
                return;

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
                    var status = row.Status == TimesheetStatusConstants.Missed ? $"{row.Status} ⚠" : row.Status;
                    var idDisplay = row.TimesheetId?.ToString() ?? "-";
                    Console.WriteLine(
                        $"{idDisplay,-8}{row.EmployeeName,-18}{row.ProjectName,-18}{row.HoursLogged,4:0.#}   {status}");
                }
            }

            ConsoleHelper.PrintDivider();
            Console.Write("[V] View timesheet detail  [B] Back — choice: ");
            var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (choice == MenuChoices.Back)
                return;

            if (choice != MenuChoices.View)
            {
                ConsoleHelper.PrintError("Invalid option.");
                continue;
            }

            Console.Write("Enter timesheet ID: ");
            if (!long.TryParse(Console.ReadLine()?.Trim(), out var timesheetId))
            {
                ConsoleHelper.PrintError("Invalid timesheet ID.");
                continue;
            }

            await ShowTimesheetDetailAsync(clients, timesheetId);
        }
    }

    private static async Task RestoreFrozenTimesheetsAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Restore Frozen Timesheet Access");

            var response = await clients.Manager.GetTeamTimesheetsAsync(null);
            if (!ApiLoadHelper.RequireLoaded(response, "Failed to load team data."))
                return;

            var frozenEmployees = response.FrozenEmployees;

            if (frozenEmployees.Count == 0)
            {
                Console.WriteLine("No employees on your team have frozen timesheet access.");
                ConsoleHelper.PrintDivider();
                Console.WriteLine("Press any key to go back...");
                Console.ReadKey(intercept: true);
                Console.WriteLine();
                return;
            }

            Console.WriteLine("Employees with frozen timesheet access:");
            ConsoleHelper.PrintDivider();
            Console.WriteLine($"{"ID",-8}{"Employee"}");
            ConsoleHelper.PrintDivider();
            foreach (var employee in frozenEmployees)
                Console.WriteLine($"{employee.EmployeeId,-8}{employee.EmployeeName}");

            ConsoleHelper.PrintDivider();
            Console.Write("[R] Restore employee  [B] Back — choice: ");
            var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (choice == MenuChoices.Back)
                return;

            if (choice != MenuChoices.Refresh)
            {
                ConsoleHelper.PrintError("Invalid option.");
                continue;
            }

            Console.Write("Enter employee ID to restore: ");
            if (!long.TryParse(Console.ReadLine()?.Trim(), out var employeeId))
            {
                ConsoleHelper.PrintError("Invalid employee ID.");
                continue;
            }

            if (frozenEmployees.All(e => e.EmployeeId != employeeId))
            {
                ConsoleHelper.PrintError("That employee is not frozen or is not on your team.");
                continue;
            }

            await ScreenRunner.RunSafeAsync(async () =>
            {
                await clients.Manager.RestoreTimesheetAccessAsync(employeeId);
                ConsoleHelper.PrintSuccess("Timesheet access restored.");
            });
        }
    }

    private static async Task ShowTimesheetDetailAsync(AppClients clients, long timesheetId)
    {
        var detail = await clients.Manager.GetTimesheetDetailAsync(timesheetId);

        if (!ApiLoadHelper.RequireLoaded(detail, "Timesheet not found."))
            return;

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
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}
