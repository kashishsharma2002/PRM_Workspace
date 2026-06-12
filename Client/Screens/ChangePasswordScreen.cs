using Client.Helpers;
using Client.HttpClients;
using Client.Models.Auth;

namespace Client.Screens;

public static class ChangePasswordScreen
{
    public static async Task<bool> RunAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("Change Password");
        Console.WriteLine("You must change your password before continuing.");
        ConsoleHelper.PrintDivider();

        Console.Write("Current password: ");
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
        var currentPassword = password;

        var newPassword = FormInputHelper.PromptPassword("New password");

        while (true)
        {
            Console.Write("Confirm new password: ");
            var confirmPassword = string.Empty;
            while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace && confirmPassword.Length > 0)
                {
                    confirmPassword = confirmPassword[..^1];
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    confirmPassword += key.KeyChar;
                    Console.Write('*');
                }
            }
            Console.WriteLine();

            if (newPassword == confirmPassword)
                break;

            ConsoleHelper.PrintError("Passwords do not match.");
        }

        try
        {
            var result = await clients.Auth.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            });

            if (result is null)
            {
                ConsoleHelper.PrintError("Password change failed.");
                return false;
            }

            SessionStore.ApplyLogin(
                result.Token,
                result.Role,
                result.FullName,
                result.UserId,
                result.EmployeeId,
                result.ManagerId,
                result.ForcePasswordChange);
            clients.SetToken(result.Token);
            ConsoleHelper.PrintSuccess("Password changed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            ErrorDisplayHelper.HandleException(ex);
            return false;
        }
    }
}
