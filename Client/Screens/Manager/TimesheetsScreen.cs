using Client.Helpers;
using Client.HttpClients;
using Client.Models.Timesheets;

namespace Client.Screens.Manager;

public static class TimesheetsScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        try
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
                    case "1":
                        await ViewTeamTimesheetsAsync(clients);
                        break;
                    case "2":
                        await RestoreFrozenTimesheetsAsync(clients);
                        break;
                    case "0":
                        return;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            }
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

    private static async Task ViewTeamTimesheetsAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("View Team Timesheets");
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

            var response = await clients.Manager.GetTeamTimesheetsAsync(weekStart);

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
                    var status = row.Status == "MISSED" ? $"{row.Status} ⚠" : row.Status;
                    var idDisplay = row.TimesheetId?.ToString() ?? "-";
                    Console.WriteLine(
                        $"{idDisplay,-8}{row.EmployeeName,-18}{row.ProjectName,-18}{row.HoursLogged,4:0.#}   {status}");
                }
            }

            ConsoleHelper.PrintDivider();
            Console.Write("[V] View timesheet detail  [B] Back — choice: ");
            var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (choice == "B")
                return;

            if (choice != "V")
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
            if (response is null)
            {
                ConsoleHelper.PrintError("Failed to load team data.");
                return;
            }

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

            if (choice == "B")
                return;

            if (choice != "R")
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

            try
            {
                await clients.Manager.RestoreTimesheetAccessAsync(employeeId);
                ConsoleHelper.PrintSuccess("Timesheet access restored.");
            }
            catch (Exception ex)
            {
                ErrorDisplayHelper.HandleException(ex);
            }
        }
    }

    private static async Task ShowTimesheetDetailAsync(AppClients clients, long timesheetId)
    {
        var detail = await clients.Manager.GetTimesheetDetailAsync(timesheetId);

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
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}
