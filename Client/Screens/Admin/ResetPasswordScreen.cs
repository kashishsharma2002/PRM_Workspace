using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ResetPasswordScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Reset User Password");

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

            Console.WriteLine($"User found: {user.FullName} ({user.Role})");
            Console.WriteLine("Password rules: min 8 chars, 1 uppercase letter, 1 number.");
            Console.WriteLine();
            Console.Write("New Temporary Password: ");
            var newPassword = ReadPassword().Trim();

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ConsoleHelper.PrintError("Password is required.");
                return;
            }

            Console.Write("Confirm New Password: ");
            var confirmPassword = ReadPassword().Trim();
            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                ConsoleHelper.PrintError("Passwords do not match.");
                return;
            }

            ConsoleHelper.PrintDivider();
            Console.Write("[S] Save  [B] Back — choice: ");
            var action = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (action != "S")
                return;

            await client.PutAsync<object>(
                $"/api/users/{user.Id}/reset-password",
                new ResetPasswordRequest { NewTemporaryPassword = newPassword },
                requireAuth: true);

            ConsoleHelper.PrintSuccess(
                "Password reset. User will be prompted to change it on next login.");
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
