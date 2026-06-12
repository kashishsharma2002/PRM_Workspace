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
            ConsoleHelper.PrintDivider();
            Console.WriteLine("1. Submit Timesheet");
            Console.WriteLine("2. View My Timesheets");
            Console.WriteLine("3. View My Allocations");
            Console.WriteLine("0. Logout");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        var defaultWeek = reminder?.ShowReminder == true ? reminder.WeekStartDate : (DateOnly?)null;
                        await SubmitTimesheetScreen.RunAsync(clients, defaultWeek);
                        break;
                    case "2":
                        await ViewTimesheetsScreen.RunAsync(clients);
                        break;
                    case "3":
                        await ViewAllocationsScreen.RunAsync(clients);
                        break;
                    case "0":
                        SessionStore.Clear();
                        clients.SetToken(null);
                        ConsoleHelper.PrintSuccess("Logged out.");
                        return false;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
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
            if (reminder?.ShowReminder == true)
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
