using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class CreateUserAccountScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Create User Account");

        Console.Write("Full Name         : ");
        var fullName = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("Email             : ");
        var email = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("Username          : ");
        var username = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("Temporary Password: ");
        var password = ReadPassword();

        Console.WriteLine("Role              : (1) Admin  (2) Manager  (3) Employee");
        Console.Write("Select role [1-3]: ");
        var roleChoice = Console.ReadLine()?.Trim();
        var role = roleChoice switch
        {
            "1" => RoleConstants.Admin,
            "2" => RoleConstants.Manager,
            "3" => RoleConstants.Employee,
            _ => string.Empty
        };

        string? department = null;
        string? designation = null;

        if (role == RoleConstants.Admin)
        {
            department = DepartmentConstants.HrOps;
            designation = DesignationConstants.SystemAdministrator;
            Console.WriteLine($"Department        : {DepartmentConstants.GetDisplayName(department)} (default for Admin)");
            Console.WriteLine($"Designation       : {DesignationConstants.GetDisplayName(designation)} (default for Admin)");
        }
        else if (role == RoleConstants.Manager)
        {
            department = OptionPickerHelper.PickFromList(
                "Department        :", DepartmentConstants.ManagerOptions, DepartmentConstants.GetDisplayName);
            designation = OptionPickerHelper.PickFromList(
                "Designation       :", DesignationConstants.ManagerOptions, DesignationConstants.GetDisplayName);
        }
        else if (role == RoleConstants.Employee)
        {
            department = OptionPickerHelper.PickFromList(
                "Department        :", DepartmentConstants.EmployeeOptions, DepartmentConstants.GetDisplayName);
            designation = OptionPickerHelper.PickFromList(
                "Designation       :", DesignationConstants.EmployeeOptions, DesignationConstants.GetDisplayName);
        }

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(role))
        {
            ConsoleHelper.PrintError("All fields are required.");
            return;
        }

        if (role is RoleConstants.Manager or RoleConstants.Employee)
        {
            if (string.IsNullOrWhiteSpace(department) || string.IsNullOrWhiteSpace(designation))
            {
                ConsoleHelper.PrintError("Department and designation must be selected from the list.");
                return;
            }
        }

        ConsoleHelper.PrintDivider();
        Console.Write("[S] Save  [B] Back — choice: ");
        var action = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (action != "S")
            return;

        try
        {
            var result = await client.PostAsync<CreateUserResponse>("/api/users", new CreateUserRequest
            {
                FullName = fullName,
                Email = email,
                Username = username,
                TemporaryPassword = password,
                Role = role,
                Department = department,
                Designation = designation
            }, requireAuth: true);

            if (result is not null)
            {
                var profileNote = role == RoleConstants.Employee
                    ? $"Resource profile created (ID: {result.EmployeeId})."
                    : "No resource profile (employees only).";
                ConsoleHelper.PrintSuccess(
                    $"Account created (User ID: {result.UserId}). {profileNote} " +
                    "User must change password on first login.");
            }
        }
        catch (SessionExpiredException ex)
        {
            ErrorDisplayHelper.HandleException(ex);
            throw;
        }
        catch (Exception ex)
        {
            ErrorDisplayHelper.HandleException(ex);
        }
    }

    private static string ReadPassword()
    {
        var password = string.Empty;
        ConsoleKeyInfo key;

        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[..^1];
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
                Console.Write('*');
            }
        }

        Console.WriteLine();
        return password;
    }
}
