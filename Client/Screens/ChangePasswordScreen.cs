using Client.Helpers;
using Client.HttpClients;
using Client.Session;

namespace Client.Screens;

public static class ChangePasswordScreen
{
    public static async Task<bool> RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Change Password");
        Console.WriteLine("You must change your password before continuing.");
        ConsoleHelper.PrintDivider();

        Console.Write("Current password: ");
        var currentPassword = ReadPassword();

        Console.Write("New password: ");
        var newPassword = ReadPassword();

        Console.Write("Confirm new password: ");
        var confirmPassword = ReadPassword();

        if (newPassword != confirmPassword)
        {
            ConsoleHelper.PrintError("New passwords do not match.");
            return false;
        }

        try
        {
            await client.PostAsync<object>("/api/auth/change-password", new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            }, requireAuth: true);

            SessionStore.ForcePasswordChange = false;
            ConsoleHelper.PrintSuccess("Password changed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError(ex.Message);
            return false;
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
