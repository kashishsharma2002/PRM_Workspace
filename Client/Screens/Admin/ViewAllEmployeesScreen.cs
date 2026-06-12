using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ViewAllEmployeesScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        try
        {
            ConsoleHelper.PrintHeader("All Employees/Resources");
            Console.Write("[F] Filter  [Enter] Show all — choice: ");
            var action = Console.ReadLine()?.Trim().ToUpperInvariant();

            string? query = null;
            if (action == "F")
            {
                Console.Write("Status (BENCH/ALLOCATED) or leave blank: ");
                var status = Console.ReadLine()?.Trim();
                Console.Write("Department or leave blank: ");
                var department = Console.ReadLine()?.Trim();

                var queryParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(status))
                    queryParts.Add($"status={Uri.EscapeDataString(status)}");
                if (!string.IsNullOrWhiteSpace(department))
                    queryParts.Add($"department={Uri.EscapeDataString(department)}");
                if (queryParts.Count > 0)
                    query = string.Join("&", queryParts);
            }

            var list = await clients.Admin.GetEmployeesAsync(query);
            if (list is null)
            {
                ConsoleHelper.PrintError("Failed to load employees.");
                return;
            }

            Console.WriteLine($"{"S. No",-6}{"ID",-8}{"User ID",-8}{"Employee/Resource Name",-24}{"Department",-12}{"Status"}");
            ConsoleHelper.PrintDivider();

            var serialNo = 0;
            foreach (var emp in list.Employees)
            {
                serialNo++;
                var status = emp.IsActive ? emp.EmploymentStatus : "Inactive";
                Console.WriteLine(
                    $"{serialNo,-6}{emp.Id,-8}{emp.UserId,-8}{emp.FullName,-24}{emp.Department ?? "-",-12}{status}");
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine($"Total: {list.Total}   |   Allocated/Active: {list.AllocatedCount}   |   Bench: {list.BenchCount}");
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
            ErrorDisplayHelper.HandleException(ex);
        }
    }
}
