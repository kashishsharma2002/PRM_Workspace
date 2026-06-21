using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class DeactivateUserScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            ConsoleHelper.PrintHeader("Deactivate User");

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

            if (!user.IsActive)
            {
                ConsoleHelper.PrintError("User is already inactive.");
                return;
            }

            Console.WriteLine($"User found: {user.FullName} ({user.Role})");
            Console.WriteLine("Status     : Active");
            Console.WriteLine();
            Console.WriteLine("Are you sure you want to deactivate this account?");
            Console.WriteLine("Deactivated users cannot log in. Their data is preserved.");
            Console.WriteLine();
            if (!FormInputHelper.ConfirmDeactivate())
                return;

            await clients.Admin.DeactivateUserAsync(user.Id);
            ConsoleHelper.PrintSuccess("User deactivated.");
        });
}
