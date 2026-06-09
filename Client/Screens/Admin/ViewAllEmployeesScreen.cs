using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ViewAllEmployeesScreen
{
    public static async Task RunAsync(RestClient client)
    {
        try
        {
            ConsoleHelper.PrintHeader("All Employees");
            Console.Write("[F] Filter  [Enter] Show all — choice: ");
            var action = Console.ReadLine()?.Trim().ToUpperInvariant();

            string endpoint = "/api/employees";
            if (action == "F")
            {
                Console.Write("Status (BENCH/ALLOCATED) or leave blank: ");
                var status = Console.ReadLine()?.Trim();
                Console.Write("Department or leave blank: ");
                var department = Console.ReadLine()?.Trim();

                var query = new List<string>();
                if (!string.IsNullOrWhiteSpace(status))
                    query.Add($"status={Uri.EscapeDataString(status)}");
                if (!string.IsNullOrWhiteSpace(department))
                    query.Add($"department={Uri.EscapeDataString(department)}");
                if (query.Count > 0)
                    endpoint += "?" + string.Join("&", query);
            }

            var list = await client.GetAsync<EmployeeListResponse>(endpoint, requireAuth: true);
            if (list is null)
            {
                ConsoleHelper.PrintError("Failed to load employees.");
                return;
            }

            Console.WriteLine($"{"ID",-6}{"Name",-20}{"Department",-14}{"Status"}");
            ConsoleHelper.PrintDivider();

            foreach (var emp in list.Employees)
            {
                var status = emp.IsActive ? emp.EmploymentStatus : "Inactive";
                Console.WriteLine($"{emp.Id,-6}{emp.FullName,-20}{emp.Department ?? "-",-14}{status}");
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine($"Total: {list.Total}   |   Allocated: {list.AllocatedCount}   |   Bench: {list.BenchCount}");
            Console.WriteLine();
            Console.WriteLine("Press any key to return...");
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }
        catch (SessionExpiredException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError(ex.Message);
        }
    }
}
