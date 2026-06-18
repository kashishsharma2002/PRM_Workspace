using Client.Helpers;
using Client.HttpClients;
using Client.Models.Timesheets;

namespace Client.Screens.Employee;

public static class EmployeeMenuScreen
{
    public static async Task<bool> RunAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintBoxHeader(
                "EMPLOYEE/RESOURCE PANEL",
                $"Welcome, {SessionStore.FullName}  |  {DateTime.Now:dd-MM-yyyy HH:mm}"
            );
            var reminder = await ShowReminderIfNeededAsync(clients);
            var isFrozen = reminder?.IsTimesheetFrozen == true;
            ConsoleHelper.PrintDivider();

            if (!isFrozen)
                Console.WriteLine("1. Submit Timesheet");

            Console.WriteLine($"{(isFrozen ? "1" : "2")}. View My Timesheets");
            Console.WriteLine($"{(isFrozen ? "2" : "3")}. View My Allocations");
            Console.WriteLine("0. Logout");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                if (!isFrozen && choice == "1")
                {
                    var defaultWeek = reminder?.ShowReminder == true ? reminder.WeekStartDate : (DateOnly?)null;
                    await SubmitTimesheetScreen.RunAsync(clients, defaultWeek);
                    continue;
                }

                if (choice == (isFrozen ? "1" : "2"))
                {
                    await ViewTimesheetsScreen.RunAsync(clients);
                    continue;
                }

                if (choice == (isFrozen ? "2" : "3"))
                {
                    await ViewAllocationsScreen.RunAsync(clients);
                    continue;
                }

                if (choice == "0")
                {
                    SessionStore.Clear();
                    clients.SetToken(null);
                    ConsoleHelper.PrintSuccess("Logged out.");
                    return false;
                }

                ConsoleHelper.PrintError("Invalid option.");
            }
            catch (SessionExpiredException)
            {
                return false;
            }
        }
    }

    private static async Task<TimesheetReminderResponse?> ShowReminderIfNeededAsync(AppClients clients)
    {
        try
        {
            var reminder = await clients.Employee.GetReminderAsync();
            if (reminder?.IsTimesheetFrozen == true)
            {
                Console.WriteLine("  🔒 Timesheet access is frozen. Contact your manager to restore submission privileges.");
            }
            else if (reminder?.ShowReminder == true)
            {
                Console.WriteLine(
                    $"  ⚠  Reminder: Timesheet for week {DateInputHelper.FormatDisplay(reminder.WeekStartDate)} has not been submitted.");
            }
            return reminder;
        }
        catch (SessionExpiredException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [WARNING] Could not load timesheet reminder: {ex.Message}");
            return null;
        }
    }
}
