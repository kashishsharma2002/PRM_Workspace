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

            var query = BuildFilterQuery(action);
            var list = await clients.Admin.GetEmployeesAsync(query);
            if (!ApiLoadHelper.RequireLoaded(list, "Failed to load employees."))
                return;

            RenderEmployeeTable(list);
            RenderSummary(list);

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

    private static string? BuildFilterQuery(string? action)
    {
        if (action != "F")
            return null;

        Console.Write("Status (BENCH/ALLOCATED) or leave blank: ");
        var status = Console.ReadLine()?.Trim();
        Console.Write("Department or leave blank: ");
        var department = Console.ReadLine()?.Trim();

        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
            queryParts.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(department))
            queryParts.Add($"department={Uri.EscapeDataString(department)}");

        return queryParts.Count > 0 ? string.Join("&", queryParts) : null;
    }

    private static void RenderEmployeeTable(EmployeeListResponse list)
    {
        Console.WriteLine(
            $"{"S. No",-6}{"ID",-8}{"User ID",-8}{"Employee/Resource Name",-24}{"Department",-24}{"Status"}");
        ConsoleHelper.PrintDivider();

        var serialNo = 0;
        foreach (var emp in list.Employees ?? [])
        {
            serialNo++;
            var status = emp.IsActive ? emp.EmploymentStatus : "Inactive";
            Console.WriteLine(
                $"{serialNo,-6}{emp.Id,-8}{emp.UserId,-8}{emp.FullName,-24}{emp.Department ?? "-",-24}{status}");
        }
    }

    private static void RenderSummary(EmployeeListResponse list)
    {
        ConsoleHelper.PrintDivider();
        Console.WriteLine(
            $"Total: {list.Total}   |   Allocated/Active: {list.AllocatedCount}   |   Bench: {list.BenchCount}");
    }
}
