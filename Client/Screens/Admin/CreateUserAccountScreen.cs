using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class CreateUserAccountScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Create User Account");

        Console.Write("Full Name         : ");
        var fullName = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("Email             : ");
        var email = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("Username          : ");
        var username = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("Temporary Password: ");
        var password = ReadPassword();

        Console.WriteLine("Role              : (1) Admin  (2) Manager  (3) Employee");
        Console.Write("Select role [1-3]: ");
        var roleChoice = Console.ReadLine()?.Trim();
        var role = roleChoice switch
        {
            "1" => "ADMIN",
            "2" => "MANAGER",
            "3" => "EMPLOYEE",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(role))
        {
            ConsoleHelper.PrintError("All fields are required.");
            return;
        }

        ConsoleHelper.PrintDivider();
        Console.Write("[S] Save  [B] Back — choice: ");
        var action = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (action != "S")
            return;

        try
        {
            var result = await client.PostAsync<CreateUserResponse>("/api/users", new CreateUserRequest
            {
                FullName = fullName,
                Email = email,
                Username = username,
                TemporaryPassword = password,
                Role = role
            }, requireAuth: true);

            if (result is not null)
            {
                ConsoleHelper.PrintSuccess(
                    $"Account created (User ID: {result.UserId}, Employee: {result.EmployeeCode}). " +
                    "User must change password on first login.");
            }
        }
        catch (SessionExpiredException ex)
        {
            ConsoleHelper.PrintError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError(ex.Message);
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
