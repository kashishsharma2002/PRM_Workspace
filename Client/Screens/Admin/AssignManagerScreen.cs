using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class AssignManagerScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Assign Manager");
        Console.WriteLine("Use User IDs from All Users (not Emp ID from All Employees).");
        Console.WriteLine();

        Console.Write("Employee User ID : ");
        if (!long.TryParse(Console.ReadLine()?.Trim(), out var employeeUserId))
        {
            ConsoleHelper.PrintError("Invalid employee user ID.");
            return;
        }

        Console.Write("Manager User ID  : ");
        if (!long.TryParse(Console.ReadLine()?.Trim(), out var managerUserId))
        {
            ConsoleHelper.PrintError("Invalid manager user ID.");
            return;
        }

        try
        {
            var list = await client.GetAsync<EmployeeListResponse>("/api/employees", requireAuth: true);
            var employee = list?.Employees.FirstOrDefault(e => e.UserId == employeeUserId);
            if (employee is null)
            {
                ConsoleHelper.PrintError(
                    "Employee not found for that User ID. Check All Users — do not use Emp ID from All Employees.");
                return;
            }

            var manager = await UserLookupHelper.ResolveUserAsync(client, managerUserId.ToString());
            if (manager is null)
            {
                ConsoleHelper.PrintError("Manager user not found. Check the ID in All Users.");
                return;
            }

            if (!manager.IsActive || !string.Equals(manager.Role, Client.Common.RoleConstants.Manager, StringComparison.OrdinalIgnoreCase))
            {
                ConsoleHelper.PrintError($"{manager.FullName} is not an active manager account.");
                return;
            }

            if (!employee.IsActive)
                Console.WriteLine("\n[NOTE] This employee is inactive and will not appear on the manager's dashboard.");

            ConsoleHelper.PrintDivider();
            Console.WriteLine($"Employee : {employee.FullName} (User ID {employee.UserId}, Emp ID {employee.Id})");
            Console.WriteLine($"Manager  : {manager.FullName} (User ID {manager.Id})");
            ConsoleHelper.PrintDivider();
            Console.Write("[S] Save  [B] Back — choice: ");
            if (Console.ReadLine()?.Trim().ToUpperInvariant() != "S")
                return;

            await client.PutAsync<object>(
                $"/api/employees/{employee.Id}/manager",
                new AssignManagerRequest { ManagerUserId = managerUserId },
                requireAuth: true);

            ConsoleHelper.PrintSuccess($"Manager assigned. {manager.FullName} will see this employee after logging in.");
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
