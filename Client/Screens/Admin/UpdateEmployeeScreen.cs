using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class UpdateEmployeeScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("Update Employee/Resource");

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

            Console.WriteLine($"Employee/Resource: {detail.FullName} ({detail.EmployeeCode})");
            Console.WriteLine($"Current Department : {FormatDepartment(detail.Department)}");
            Console.WriteLine($"Current Designation: {FormatDesignation(detail.Designation)}");
            Console.WriteLine();

            Console.WriteLine("New Department:");
            var department = OptionPickerHelper.PickFromList(
                string.Empty, PrependKeepOption(DepartmentConstants.EmployeeOptions), FormatDepartmentPick);
            if (department == KeepCurrent)
                department = null;

            Console.WriteLine("New Designation:");
            var designation = OptionPickerHelper.PickFromList(
                string.Empty, PrependKeepOption(DesignationConstants.EmployeeOptions), FormatDesignationPick);
            if (designation == KeepCurrent)
                designation = null;

            if (department is null && designation is null)
            {
                ConsoleHelper.PrintError("At least one field must be changed.");
                return;
            }

            ConsoleHelper.PrintDivider();
            Console.Write("[S] Save  [B] Back — choice: ");
            if (Console.ReadLine()?.Trim().ToUpperInvariant() != "S")
                return;

            await clients.Admin.UpdateEmployeeAsync(
                employeeId,
                new UpdateEmployeeRequest
                {
                    Department = department,
                    Designation = designation
                });

            ConsoleHelper.PrintSuccess("Employee/Resource updated.");
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

    private const string KeepCurrent = "__KEEP__";

    private static string[] PrependKeepOption(string[] options) =>
        [KeepCurrent, ..options];

    private static string FormatDepartment(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : DepartmentConstants.GetDisplayName(value);

    private static string FormatDesignation(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : DesignationConstants.GetDisplayName(value);

    private static string FormatDepartmentPick(string value) =>
        value == KeepCurrent ? "Keep current" : FormatDepartment(value);

    private static string FormatDesignationPick(string value) =>
        value == KeepCurrent ? "Keep current" : FormatDesignation(value);
}
