using Client.Common;
using Client.Helpers;
using Client.HttpClients;
using Client.Models.Users;

namespace Client.Screens.Admin;

public static class ChangeUserRoleScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            ConsoleHelper.PrintHeader("Change User Role");

            Console.Write("Enter Username or User ID: ");
            var input = Console.ReadLine()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                ConsoleHelper.PrintError("Username or User ID is required.");
                return;
            }

            var user = await UserLookupHelper.ResolveUserAsync(clients, input);
            if (user is null)
            {
                ConsoleHelper.PrintError("User not found.");
                return;
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine($"User         : {user.FullName} ({user.Username})");
            Console.WriteLine($"Current Role : {RoleDisplayHelper.Format(user.Role)}");
            Console.WriteLine($"Status       : {(user.IsActive ? "Active" : "Inactive")}");
            ConsoleHelper.PrintDivider();
            Console.WriteLine("New Role     : (1) Admin  (2) Manager  (3) Employee/Resource");
            Console.Write("Select role [1-3]: ");
            var roleChoice = Console.ReadLine()?.Trim();
            var newRole = RoleDisplayHelper.ResolveSystemRoleChoice(roleChoice);

            if (string.IsNullOrEmpty(newRole))
            {
                ConsoleHelper.PrintError("Invalid role selection.");
                return;
            }

            if (string.Equals(user.Role, newRole, StringComparison.OrdinalIgnoreCase))
            {
                ConsoleHelper.PrintError("User already has this role.");
                return;
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine(
                $"Change {user.FullName}'s role from {RoleDisplayHelper.Format(user.Role)} to {RoleDisplayHelper.Format(newRole)}?");
            Console.WriteLine("Note: User must log out and back in for menu access to update.");
            if (!FormInputHelper.ConfirmSave())
                return;

            await clients.Admin.UpdateUserRoleAsync(user.Id, new UpdateUserRoleRequest { Role = newRole });
            ConsoleHelper.PrintSuccess($"Role updated to {RoleDisplayHelper.Format(newRole)}.");
            Console.WriteLine("Ask the user to log out and sign in again.");
        });
}
