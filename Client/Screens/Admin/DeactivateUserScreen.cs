using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class DeactivateUserScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Deactivate User");

        Console.Write("Enter Username or User ID: ");
        var input = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            ConsoleHelper.PrintError("Username or User ID is required.");
            return;
        }

        try
        {
            var user = await UserLookupHelper.ResolveUserAsync(client, input);
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
            Console.Write("[Y] Yes, Deactivate  [B] Back — choice: ");
            var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (confirm != "Y")
                return;

            await client.PutAsync<object>($"/api/users/{user.Id}/deactivate", new { }, requireAuth: true);
            ConsoleHelper.PrintSuccess("User deactivated.");
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
