using System.Text.RegularExpressions;

namespace Client.Helpers;

public static class FormInputHelper
{
    private const int MinPasswordLength = 8;
    private const int MaxAllocationPercentage = 100;
    private const int MinAllocationPercentage = 1;
    private const int MaxEmailLength = 255;

    public static string PromptRequired(string fieldName, string? defaultValue = null)
    {
        while (true)
        {
            Console.Write($"{fieldName}: ");
            var input = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(input))
                return input;

            if (!string.IsNullOrEmpty(defaultValue))
                return defaultValue;

            ConsoleHelper.PrintError($"{fieldName} is required.");
        }
    }

    public static string PromptEmail(string fieldName = "Email")
    {
        while (true)
        {
            var email = PromptRequired(fieldName);
            if (IsValidEmail(email))
                return email;

            ConsoleHelper.PrintError("Email must be a valid email address.");
        }
    }

    public static string PromptPassword(string fieldName = "Password")
    {
        while (true)
        {
            Console.Write($"{fieldName}: ");
            var password = ReadPassword();

            if (string.IsNullOrEmpty(password))
            {
                ConsoleHelper.PrintError("Password cannot be empty.");
                continue;
            }

            if (password.Length < MinPasswordLength)
            {
                ConsoleHelper.PrintError($"Password must be at least {MinPasswordLength} characters.");
                continue;
            }

            if (!password.Any(char.IsUpper))
            {
                ConsoleHelper.PrintError("Password must contain at least one uppercase letter.");
                continue;
            }

            if (!password.Any(char.IsDigit))
            {
                ConsoleHelper.PrintError("Password must contain at least one number.");
                continue;
            }

            return password;
        }
    }

    public static decimal PromptPercentage(string fieldName = "Percentage")
    {
        while (true)
        {
            Console.Write($"{fieldName} ({MinAllocationPercentage}-{MaxAllocationPercentage}): ");
            if (decimal.TryParse(Console.ReadLine()?.Trim(), out var percentage) &&
                percentage >= MinAllocationPercentage && percentage <= MaxAllocationPercentage)
                return percentage;

            ConsoleHelper.PrintError($"{fieldName} must be a number between {MinAllocationPercentage} and {MaxAllocationPercentage}.");
        }
    }

    public static DateOnly PromptDate(string fieldName = "Date", string format = "(DD-MM-YYYY)")
    {
        while (true)
        {
            Console.Write($"{fieldName} {format}: ");
            if (DateInputHelper.TryParseToIso(Console.ReadLine() ?? string.Empty, out var isoDate))
                return DateOnly.Parse(isoDate);

            ConsoleHelper.PrintError($"Invalid {fieldName}. Use format DD-MM-YYYY.");
        }
    }

    public static DateOnly PromptDateNotBeforeToday(string fieldName = "Date", string format = "(DD-MM-YYYY)")
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        while (true)
        {
            var date = PromptDate(fieldName, format);
            if (date >= today)
                return date;

            ConsoleHelper.PrintError($"{fieldName} cannot be in the past (today is {DateInputHelper.FormatDisplay(today)}).");
        }
    }

    public static (DateOnly startDate, DateOnly endDate) PromptDateRange(
        string startFieldName = "Start Date",
        string endFieldName = "End Date",
        string format = "(DD-MM-YYYY)")
    {
        var startDate = PromptDate(startFieldName, format);
        while (true)
        {
            var endDate = PromptDate(endFieldName, format);
            if (endDate > startDate)
                return (startDate, endDate);

            ConsoleHelper.PrintError($"{endFieldName} must be after {startFieldName}.");
        }
    }

    public static long PromptId(string fieldName = "ID")
    {
        while (true)
        {
            Console.Write($"{fieldName}: ");
            if (long.TryParse(Console.ReadLine()?.Trim(), out var id) && id > 0)
                return id;

            ConsoleHelper.PrintError($"{fieldName} must be a valid positive number.");
        }
    }

    public static int PromptInt(string fieldName, int? minValue = null, int? maxValue = null)
    {
        while (true)
        {
            Console.Write($"{fieldName}: ");
            if (!int.TryParse(Console.ReadLine()?.Trim(), out var value))
            {
                ConsoleHelper.PrintError($"{fieldName} must be a valid number.");
                continue;
            }

            if (minValue.HasValue && value < minValue)
            {
                ConsoleHelper.PrintError($"{fieldName} must be at least {minValue}.");
                continue;
            }

            if (maxValue.HasValue && value > maxValue)
            {
                ConsoleHelper.PrintError($"{fieldName} must be at most {maxValue}.");
                continue;
            }

            return value;
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

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > MaxEmailLength)
            return false;

        try
        {
            var emailRegex = new Regex(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(250));
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }
}
