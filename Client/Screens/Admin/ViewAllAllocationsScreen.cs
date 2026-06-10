using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ViewAllAllocationsScreen
{
    public static async Task RunAsync(RestClient client)
    {
        try
        {
            ConsoleHelper.PrintHeader("All Allocations");
            Console.Write("[F] Filter  [Enter] Show active — choice: ");
            var action = Console.ReadLine()?.Trim().ToUpperInvariant();

            string endpoint = "/api/allocations";
            if (action == "F")
            {
                Console.Write("Employee ID (blank for all): ");
                var empInput = Console.ReadLine()?.Trim();
                Console.Write("Project ID (blank for all): ");
                var projInput = Console.ReadLine()?.Trim();

                var query = new List<string>();
                if (long.TryParse(empInput, out var empId))
                    query.Add($"employeeId={empId}");
                if (long.TryParse(projInput, out var projId))
                    query.Add($"projectId={projId}");
                if (query.Count > 0)
                    endpoint += "?" + string.Join("&", query);
            }

            var list = await client.GetAsync<AllocationListResponse>(endpoint, requireAuth: true);
            if (list is null)
            {
                ConsoleHelper.PrintError("Failed to load allocations.");
                return;
            }

            Console.WriteLine($"{"Employee",-18}{"Project",-18}{"%",-6}{"From",-12}{"To"}");
            ConsoleHelper.PrintDivider();

            foreach (var allocation in list.Allocations)
            {
                Console.WriteLine($"{allocation.EmployeeName,-18}{allocation.ProjectName,-18}" +
                    $"{allocation.AllocationPercentage}%{"",-4}" +
                    $"{DateInputHelper.FormatDisplay(allocation.AllocationStartDate),-12}" +
                    $"{DateInputHelper.FormatDisplay(allocation.AllocationEndDate)}");
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine($"Total Active Allocations: {list.TotalActiveCount}");
            Console.WriteLine();
            Console.WriteLine("Press any key to return...");
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }
        catch (SessionExpiredException) { throw; }
        catch (Exception ex) { ErrorDisplayHelper.HandleException(ex); }
    }
}
