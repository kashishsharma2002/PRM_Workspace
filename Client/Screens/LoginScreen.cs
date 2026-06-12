using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens;

public static class LoginScreen
{
    public static async Task<bool> RunAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("PRM Login");

        Console.Write("Username: ");
        var username = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("Password: ");
        var password = ReadPassword();

        try
        {
            var result = await clients.Auth.LoginAsync(new LoginRequest
            {
                Username = username,
                Password = password
            });

            if (result is null)
            {
                ConsoleHelper.PrintError("Login failed.");
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
            ConsoleHelper.PrintSuccess($"Welcome, {result.FullName}!");
            return true;
        }
        catch (SessionExpiredException ex)
        {
            ErrorDisplayHelper.HandleException(ex);
            return false;
        }
        catch (Exception ex)
        {
            ErrorDisplayHelper.HandleException(ex);
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
