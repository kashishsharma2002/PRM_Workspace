using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class AssignManagerScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Assign Manager");

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
                ConsoleHelper.PrintError("Employee not found for the given user ID.");
                return;
            }

            ConsoleHelper.PrintDivider();
            Console.Write("[S] Save  [B] Back — choice: ");
            if (Console.ReadLine()?.Trim().ToUpperInvariant() != "S")
                return;

            await client.PutAsync<object>(
                $"/api/employees/{employee.Id}/manager",
                new AssignManagerRequest { ManagerUserId = managerUserId },
                requireAuth: true);

            ConsoleHelper.PrintSuccess("Manager assigned.");
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
