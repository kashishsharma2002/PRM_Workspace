using Client.Helpers;
using Client.HttpClients;
using Client.Models.Employees;

namespace Client.Screens.Manager;

public static class ResourceDashboardScreen
{
    public static async Task RunAsync(RestClient client)
    {
        while (true)
        {
            try
            {
                ConsoleHelper.PrintHeader("Resource Dashboard");
                var dashboard = await client.GetAsync<TeamDashboard>("/api/employees/my-team", requireAuth: true);
                if (dashboard is null)
                {
                    ConsoleHelper.PrintError("Could not load team dashboard.");
                    return;
                }

                Console.WriteLine($"ON BENCH  ({dashboard.BenchCount} employees available)");
                ConsoleHelper.PrintDivider();
                Console.WriteLine($"{"ID",-6}{"Name",-18}{"Department",-14}Skills");
                foreach (var employee in dashboard.BenchEmployees)
                {
                    var skills = string.Join(", ", employee.Skills);
                    Console.WriteLine($"{employee.Id,-6}{employee.Name,-18}{employee.Department ?? "-",-14}{skills}");
                }

                Console.WriteLine();
                Console.WriteLine("ACTIVE EMPLOYEES");
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
                Console.WriteLine("[D] Drill into employee details     [B] Back");
                Console.Write("Enter option: ");
                var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

                if (choice == "B")
                    return;

                if (choice != "D")
                {
                    ConsoleHelper.PrintError("Invalid option.");
                    continue;
                }

                Console.Write("Enter Employee ID: ");
                if (!long.TryParse(Console.ReadLine()?.Trim(), out var employeeId))
                {
                    ConsoleHelper.PrintError("Invalid employee ID.");
                    continue;
                }

                await ShowMemberDetailAsync(client, employeeId);
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

    private static async Task ShowMemberDetailAsync(RestClient client, long employeeId)
    {
        var detail = await client.GetAsync<TeamMemberDetail>(
            $"/api/employees/my-team/{employeeId}",
            requireAuth: true);

        if (detail is null)
        {
            ConsoleHelper.PrintError("Employee not found.");
            return;
        }

        ConsoleHelper.PrintDivider();
        Console.WriteLine($"── {detail.FullName} ─────────────────────────────────");
        Console.WriteLine($"Department     : {detail.Department ?? "-"}");
        Console.WriteLine($"Current Status : {detail.EmploymentStatus} ({detail.TotalUtilizationPercentage:0.#}%)");
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
        Console.WriteLine("Press any key to go back...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}
