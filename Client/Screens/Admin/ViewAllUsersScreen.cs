using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ViewAllUsersScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            var list = await clients.Admin.GetUsersAsync();
            if (!ApiLoadHelper.RequireLoaded(list, "Failed to load users."))
                return;

            ConsoleHelper.PrintHeader("All Users");
            Console.WriteLine($"{"ID",-6}{"Username",-18}{"Role",-12}{"Status"}");
            ConsoleHelper.PrintDivider();

            foreach (var user in list.Users)
            {
                var status = user.IsActive ? "Active" : "Inactive";
                Console.WriteLine($"{user.Id,-6}{user.Username,-18}{user.Role,-12}{status}");
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine($"Total: {list.Total}   |   Active: {list.ActiveCount}   |   Inactive: {list.InactiveCount}");
            ConsoleHelper.PrintDivider();
            Console.Write("[R] Reactivate a user  [B] Back — choice: ");
            var action = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (action == "R")
                await ReactivateUserAsync(clients, list);
        });

    private static async Task ReactivateUserAsync(AppClients clients, UserListResponse list)
    {
        Console.Write("Enter User ID to reactivate: ");
        if (!long.TryParse(Console.ReadLine()?.Trim(), out var userId))
        {
            ConsoleHelper.PrintError("Invalid user ID.");
            return;
        }

        var user = list.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null)
        {
            ConsoleHelper.PrintError("User not found.");
            return;
        }

        if (user.IsActive)
        {
            ConsoleHelper.PrintError("User is already active.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"User: {user.FullName} ({user.Role}) — currently Inactive");
        Console.WriteLine();
        if (!FormInputHelper.ConfirmYes("Reactivate this account? [Y] Yes  [B] Cancel — choice: "))
            return;

        try
        {
            await clients.Admin.ReactivateUserAsync(userId);
            ConsoleHelper.PrintSuccess(
                $"Account reactivated. {user.FullName} can now log in.");
            Console.WriteLine(
                "Note: Previous allocations are NOT restored. Admin must re-allocate manually if needed.");
        }
        catch (Exception ex)
        {
            ErrorDisplayHelper.HandleException(ex);
        }
    }
}
