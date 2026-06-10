using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class UpdateEmployeeScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Update Employee");

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

            Console.WriteLine($"Employee: {detail.FullName} ({detail.EmployeeCode})");
            Console.WriteLine($"Current Department : {detail.Department ?? "-"}");
            Console.WriteLine($"Current Designation: {detail.Designation ?? "-"}");
            Console.WriteLine();

            Console.Write("New Department (blank to keep): ");
            var department = Console.ReadLine()?.Trim();

            Console.Write("New Designation (blank to keep): ");
            var designation = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(department) && string.IsNullOrWhiteSpace(designation))
            {
                ConsoleHelper.PrintError("At least one field must be provided.");
                return;
            }

            ConsoleHelper.PrintDivider();
            Console.Write("[S] Save  [B] Back — choice: ");
            if (Console.ReadLine()?.Trim().ToUpperInvariant() != "S")
                return;

            await client.PutAsync<object>(
                $"/api/employees/{employeeId}",
                new UpdateEmployeeRequest
                {
                    Department = string.IsNullOrWhiteSpace(department) ? null : department,
                    Designation = string.IsNullOrWhiteSpace(designation) ? null : designation
                },
                requireAuth: true);

            ConsoleHelper.PrintSuccess("Employee updated.");
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
