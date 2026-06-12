using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class DeactivateEmployeeScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("Deactivate Employee/Resource");

        Console.Write("Enter Employee/Resource ID: ");
        if (!long.TryParse(Console.ReadLine()?.Trim(), out var employeeId))
        {
            ConsoleHelper.PrintError("Invalid employee/resource ID.");
            return;
        }

        try
        {
            var detail = await clients.Admin.GetEmployeeAsync(employeeId);
            if (detail is null)
            {
                ConsoleHelper.PrintError("Employee/Resource not found.");
                return;
            }

            if (!detail.IsActive)
            {
                ConsoleHelper.PrintError("Employee/Resource is already inactive.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"── {detail.FullName} ─────────────────────────────────");
            Console.WriteLine($"Department : {detail.Department ?? "-"}");
            Console.WriteLine($"Status     : {detail.EmploymentStatus}");

            if (detail.ActiveAllocations.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"⚠  Warning: This employee/resource has {detail.ActiveAllocations.Count} active allocation(s).");
                Console.WriteLine("   Ending their employment/allocations will remove them from:");
                foreach (var allocation in detail.ActiveAllocations)
                {
                    Console.WriteLine($"     - {allocation.ProjectName}  ({allocation.AllocationPercentage}%,  ends {allocation.AllocationEndDate:dd-MMM-yy})");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Are you sure you want to deactivate this employee/resource?");
            Console.WriteLine("This will: set is_active = false, end all active allocations today,");
            Console.WriteLine("and block their login account.");
            Console.WriteLine();
            Console.Write("[Y] Yes, Deactivate  [B] Back — choice: ");
            if (Console.ReadLine()?.Trim().ToUpperInvariant() != "Y")
                return;

            await clients.Admin.DeactivateEmployeeAsync(employeeId);
            ConsoleHelper.PrintSuccess("Employee/Resource deactivated.");
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
