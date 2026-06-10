using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class DeactivateEmployeeScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Deactivate Employee");

        Console.Write("Enter Employee ID: ");
        if (!long.TryParse(Console.ReadLine()?.Trim(), out var employeeId))
        {
            ConsoleHelper.PrintError("Invalid employee ID.");
            return;
        }

        try
        {
            var detail = await client.GetAsync<EmployeeDetail>($"/api/employees/{employeeId}", requireAuth: true);
            if (detail is null)
            {
                ConsoleHelper.PrintError("Employee not found.");
                return;
            }

            if (!detail.IsActive)
            {
                ConsoleHelper.PrintError("Employee is already inactive.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"── {detail.FullName} ─────────────────────────────────");
            Console.WriteLine($"Department : {detail.Department ?? "-"}");
            Console.WriteLine($"Status     : {detail.EmploymentStatus}");

            if (detail.ActiveAllocations.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"⚠  Warning: This employee has {detail.ActiveAllocations.Count} active allocation(s).");
                Console.WriteLine("   Ending their employment will remove them from:");
                foreach (var allocation in detail.ActiveAllocations)
                {
                    Console.WriteLine($"     - {allocation.ProjectName}  ({allocation.AllocationPercentage}%,  ends {allocation.AllocationEndDate:dd-MMM-yy})");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Are you sure you want to deactivate this employee?");
            Console.WriteLine("This will: set is_active = false, end all active allocations today,");
            Console.WriteLine("and block their login account.");
            Console.WriteLine();
            Console.Write("[Y] Yes, Deactivate  [B] Back — choice: ");
            if (Console.ReadLine()?.Trim().ToUpperInvariant() != "Y")
                return;

            await client.PutAsync<object>($"/api/employees/{employeeId}/deactivate", new { }, requireAuth: true);
            ConsoleHelper.PrintSuccess("Employee deactivated.");
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
