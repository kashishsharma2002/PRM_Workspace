using Client.Common;
using Client.Helpers;
using Client.HttpClients;
using Client.Models.Employees;

namespace Client.Screens.Manager;

public static class ResourceDashboardScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        while (true)
        {
            var exitScreen = false;
            await ScreenRunner.RunSafeAsync(async () =>
            {
                ConsoleHelper.PrintHeader("Employee/Resource Dashboard");
                var dashboard = await clients.Manager.GetTeamDashboardAsync();
                if (!ApiLoadHelper.RequireLoaded(dashboard, "Could not load team dashboard."))
                {
                    exitScreen = true;
                    return;
                }

                Console.WriteLine($"ON BENCH  ({dashboard.BenchCount} employees/resources available)");
                ConsoleHelper.PrintDivider();
                Console.WriteLine($"{"ID",-6}{"Name",-18}{"Department",-14}Skills");
                foreach (var employee in dashboard.BenchEmployees)
                {
                    var skills = string.Join(", ", employee.Skills);
                    Console.WriteLine($"{employee.Id,-6}{employee.Name,-18}{employee.Department ?? "-",-14}{skills}");
                }

                Console.WriteLine();
                Console.WriteLine("ACTIVE EMPLOYEES/RESOURCES");
                ConsoleHelper.PrintDivider();
                Console.WriteLine($"{"ID",-6}{"Name",-18}{"Alloc %",-10}Availability");
                foreach (var employee in dashboard.ActiveEmployees)
                {
                    var availability = employee.AllocationPercentage >= 100
                        ? "FULL"
                        : $"{employee.AvailabilityPercentage:0.#}% free";
                    Console.WriteLine($"{employee.Id,-6}{employee.Name,-18}{employee.AllocationPercentage,5:0.#}%   {availability}");
                }

                ConsoleHelper.PrintDivider();
                Console.WriteLine($"Bench: {dashboard.BenchCount}   |   Partial: {dashboard.PartialCount}");
                ConsoleHelper.PrintDivider();
                Console.WriteLine("[D] Drill into employee/resource details     [B] Back");
                Console.Write("Enter option: ");
                var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

                if (choice == MenuChoices.Back)
                {
                    exitScreen = true;
                    return;
                }

                if (choice != MenuChoices.Details)
                {
                    ConsoleHelper.PrintError("Invalid option.");
                    return;
                }

                Console.Write("Enter Employee/Resource ID: ");
                if (!long.TryParse(Console.ReadLine()?.Trim(), out var employeeId))
                {
                    ConsoleHelper.PrintError("Invalid employee/resource ID.");
                    return;
                }

                await ShowMemberDetailAsync(clients, employeeId);
            });

            if (exitScreen)
                return;
        }
    }

    private static async Task ShowMemberDetailAsync(AppClients clients, long employeeId)
    {
        var detail = await clients.Manager.GetTeamMemberDetailAsync(employeeId);

        if (!ApiLoadHelper.RequireLoaded(detail, "Employee/Resource not found."))
            return;

        ConsoleHelper.PrintDivider();
        Console.WriteLine($"── {detail.FullName} ─────────────────────────────────");
        Console.WriteLine($"Department     : {detail.Department ?? "-"}");
        Console.WriteLine($"Current Status : {detail.EmploymentStatus} (Employee/Resource) ({detail.TotalUtilizationPercentage:0.#}%)");
        if (detail.IsTimesheetFrozen)
            Console.WriteLine("Timesheet Access: FROZEN");
        Console.WriteLine($"Profile Skills : {string.Join(", ", detail.Skills.Select(s => s.SkillName))}");
        Console.WriteLine();
        Console.WriteLine("Active Allocations:");
        Console.WriteLine($"  {"Project",-16}{"%",-6}{"From",-14}To");
        foreach (var allocation in detail.ActiveAllocations)
        {
            Console.WriteLine(
                $"  {allocation.ProjectName,-16}{allocation.AllocationPercentage,4:0.#}%  " +
                $"{DateInputHelper.FormatDisplay(allocation.AllocationStartDate),-14}" +
                $"{DateInputHelper.FormatDisplay(allocation.AllocationEndDate)}");
        }

        if (detail.RecentActivityTags.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Recent Activity Tags (last 4 weeks):");
            Console.WriteLine($"  {string.Join(", ", detail.RecentActivityTags)}");
        }

        ConsoleHelper.PrintDivider();
        if (detail.IsTimesheetFrozen)
        {
            Console.Write("[R] Restore timesheet access     ");
        }

        Console.WriteLine("Press any key to go back...");
        if (detail.IsTimesheetFrozen)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is 'r' or 'R')
            {
                Console.WriteLine();
                await ScreenRunner.RunSafeAsync(async () =>
                {
                    await clients.Manager.RestoreTimesheetAccessAsync(employeeId);
                    ConsoleHelper.PrintSuccess("Timesheet access restored.");
                });
            }
            else
            {
                Console.WriteLine();
            }
        }
        else
        {
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }
    }
}
