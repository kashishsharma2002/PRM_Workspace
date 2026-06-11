using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Employee;

public static class EmployeeMenuScreen
{
    public static async Task<bool> RunAsync(RestClient client)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader($"Welcome, {SessionStore.FullName}!");
            await ShowReminderIfNeededAsync(client);
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
                        await SubmitTimesheetScreen.RunAsync(client);
                        break;
                    case "2":
                        await ViewTimesheetsScreen.RunAsync(client);
                        break;
                    case "3":
                        await ViewAllocationsScreen.RunAsync(client);
                        break;
                    case "0":
                        SessionStore.Clear();
                        client.SetToken(null);
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

    private static async Task ShowReminderIfNeededAsync(RestClient client)
    {
        try
        {
            var reminder = await client.GetAsync<TimesheetReminderResponse>("/api/timesheets/reminder", requireAuth: true);
            if (reminder?.ShowReminder == true)
            {
                Console.WriteLine(
                    $"  !  Reminder: Timesheet for week {DateInputHelper.FormatDisplay(reminder.WeekStartDate)} has not been submitted.");
            }
        }
        catch
        {
        }
    }
}
