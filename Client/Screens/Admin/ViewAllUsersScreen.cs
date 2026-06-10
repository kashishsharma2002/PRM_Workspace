using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ViewAllUsersScreen
{
    public static async Task RunAsync(RestClient client)
    {
        try
        {
            var list = await client.GetAsync<UserListResponse>("/api/users", requireAuth: true);
            if (list is null)
            {
                ConsoleHelper.PrintError("Failed to load users.");
                return;
            }

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
                await ReactivateUserAsync(client, list);
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

    private static async Task ReactivateUserAsync(RestClient client, UserListResponse list)
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
        Console.Write("Reactivate this account? [Y] Yes  [B] Cancel — choice: ");
        var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (confirm != "Y")
            return;

        try
        {
            await client.PutAsync<object>($"/api/users/{userId}/reactivate", new { }, requireAuth: true);
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
