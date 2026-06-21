using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ViewAllAllocationsScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            ConsoleHelper.PrintHeader("All Allocations");
            Console.Write("[F] Filter  [Enter] Show active — choice: ");
            var action = Console.ReadLine()?.Trim().ToUpperInvariant();

            string? query = null;
            if (action == MenuChoices.Filter)
            {
                Console.Write("Employee ID (blank for all): ");
                var empInput = Console.ReadLine()?.Trim();
                Console.Write("Project ID (blank for all): ");
                var projInput = Console.ReadLine()?.Trim();

                var queryParts = new List<string>();
                if (long.TryParse(empInput, out var empId))
                    queryParts.Add($"employeeId={empId}");
                if (long.TryParse(projInput, out var projId))
                    queryParts.Add($"projectId={projId}");
                if (queryParts.Count > 0)
                    query = string.Join("&", queryParts);
            }

            var list = await clients.Admin.GetAllocationsAsync(query);
            if (!ApiLoadHelper.RequireLoaded(list, "Failed to load allocations."))
                return;

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
        });
}
