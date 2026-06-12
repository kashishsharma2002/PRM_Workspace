using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class CreateUserAccountScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("Create User Account");

        var fullName = FormInputHelper.PromptRequired("Full Name");
        var email = FormInputHelper.PromptEmail();
        var username = FormInputHelper.PromptRequired("Username");
        var password = FormInputHelper.PromptPassword("Temporary Password");

        Console.WriteLine("Role              : (1) Admin  (2) Manager  (3) Employee/Resource");
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

        if (string.IsNullOrWhiteSpace(role))
        {
            ConsoleHelper.PrintError("Role is required.");
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
            var result = await clients.Admin.CreateUserAsync(new CreateUserRequest
            {
                FullName = fullName,
                Email = email,
                Username = username,
                TemporaryPassword = password,
                Role = role,
                Department = department,
                Designation = designation
            });

            if (result is not null)
            {
                var profileNote = role == RoleConstants.Employee
                    ? $"Resource profile created (ID: {result.EmployeeId})."
                    : "No resource profile (employees/resources only).";
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
}
