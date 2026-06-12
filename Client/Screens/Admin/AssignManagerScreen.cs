using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class AssignManagerScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("Assign Manager");
        Console.WriteLine("Enter the Resource Profile ID from All Employees/Resources.");
        Console.WriteLine();

        Console.Write("Employee/Resource ID : ");
        if (!long.TryParse(Console.ReadLine()?.Trim(), out var employeeProfileId))
        {
            ConsoleHelper.PrintError("Invalid employee/resource ID.");
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
            var list = await clients.Admin.GetEmployeesAsync();
            var employee = list?.Employees.FirstOrDefault(e => e.Id == employeeProfileId);
            if (employee is null)
            {
                ConsoleHelper.PrintError(
                    "Employee/Resource not found for that ID. Check All Employees/Resources.");
                return;
            }

            var manager = await UserLookupHelper.ResolveUserAsync(clients, managerUserId.ToString());
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
                Console.WriteLine("\n[NOTE] This employee/resource is inactive and will not appear on the manager's dashboard.");

            ConsoleHelper.PrintDivider();
            Console.WriteLine($"Employee/Resource : {employee.FullName} (Profile ID {employee.Id}, User ID {employee.UserId})");
            Console.WriteLine($"Manager           : {manager.FullName} (User ID {manager.Id})");
            ConsoleHelper.PrintDivider();
            Console.Write("[S] Save  [B] Back — choice: ");
            if (Console.ReadLine()?.Trim().ToUpperInvariant() != "S")
                return;

            await clients.Admin.AssignManagerAsync(
                employee.Id,
                new AssignManagerRequest { ManagerUserId = managerUserId });

            ConsoleHelper.PrintSuccess($"Manager assigned. {manager.FullName} will see this employee/resource after logging in.");
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
