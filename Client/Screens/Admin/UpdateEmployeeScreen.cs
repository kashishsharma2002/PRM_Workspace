using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class UpdateEmployeeScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            ConsoleHelper.PrintHeader("Update Employee/Resource");

            Console.Write("Enter Employee/Resource ID: ");
            if (!long.TryParse(Console.ReadLine()?.Trim(), out var employeeId))
            {
                ConsoleHelper.PrintError("Invalid employee/resource ID.");
                return;
            }

            var detail = await clients.Admin.GetEmployeeAsync(employeeId);
            if (!ApiLoadHelper.RequireLoaded(detail, "Employee/Resource not found."))
                return;

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
                ConsoleHelper.PrintSuccess("No changes made.");
                return;
            }

            ConsoleHelper.PrintDivider();
            if (!FormInputHelper.ConfirmSave())
                return;

            await clients.Admin.UpdateEmployeeAsync(
                employeeId,
                new UpdateEmployeeRequest
                {
                    Department = department,
                    Designation = designation
                });

            ConsoleHelper.PrintSuccess("Employee/Resource updated.");
        });

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
