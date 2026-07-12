using Client.Common;
using Client.Helpers;
using Client.HttpClients;
using Client.Models.SystemConfig;

namespace Client.Screens.Admin;

public static class SystemConfigScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            while (true)
            {
                var config = await clients.Admin.GetSystemConfigAsync();
                if (!ApiLoadHelper.RequireLoaded(config, "Failed to load configuration."))
                    return;

                ConsoleHelper.PrintHeader("System Configuration");
                Console.WriteLine("Current Settings:");
                Console.WriteLine($"  LLM Provider        :  {config.LlmProvider}");
                Console.WriteLine($"  LLM API Key         :  {(string.IsNullOrEmpty(config.LlmApiKeyMasked) ? "(not set)" : config.LlmApiKeyMasked)}");
                Console.WriteLine($"  Scheduler Interval  :  {config.SchedulerIntervalHours} hours");
                Console.WriteLine($"  Max Weekly Hours    :  {config.MaxWeeklyHours}");
                Console.WriteLine($"  Timesheet Deadline  :  {config.TimesheetDeadlineWorkingDaysAfterWeekEnd} working day(s) after week end");
                ConsoleHelper.PrintDivider();
                Console.WriteLine("1. Update LLM API Key");
                Console.WriteLine("2. Change LLM Provider  (Gemini / Groq / Gemma)");
                Console.WriteLine("3. Update Scheduler Interval");
                Console.WriteLine("4. Update Max Weekly Hours");
                Console.WriteLine("5. Update Timesheet Deadline Offset");
                Console.WriteLine("0. Back");
                Console.Write("Enter option: ");
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case MenuChoices.One:
                        await UpdateApiKeyAsync(clients, config.LlmProvider);
                        break;
                    case MenuChoices.Two:
                        await UpdateProviderAsync(clients, config.LlmProvider);
                        break;
                    case MenuChoices.Three:
                        await UpdateSchedulerAsync(clients);
                        break;
                    case "4":
                        await UpdateMaxHoursAsync(clients);
                        break;
                    case "5":
                        await UpdateTimesheetDeadlineAsync(clients, config.TimesheetDeadlineWorkingDaysAfterWeekEnd);
                        break;
                    case MenuChoices.Exit:
                        return;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            }
        });

    private static async Task UpdateApiKeyAsync(AppClients clients, string currentProvider)
    {
        Console.Write("New LLM API Key: ");
        var key = ReadMaskedInput();
        if (string.IsNullOrWhiteSpace(key))
        {
            if (currentProvider.Equals(LlmProviders.Gemma, StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("Gemma will use an empty apikey header. Confirm? (y/n): ");
                if (!string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                    return;

                await clients.Admin.UpdateSystemConfigAsync(
                    new UpdateSystemConfigRequest { LlmApiKey = string.Empty });
                ConsoleHelper.PrintSuccess("LLM API key cleared for Gemma.");
                return;
            }

            ConsoleHelper.PrintError("API key is required.");
            return;
        }

        await clients.Admin.UpdateSystemConfigAsync(new UpdateSystemConfigRequest { LlmApiKey = key });
        ConsoleHelper.PrintSuccess("LLM API key updated.");
    }

    private static async Task UpdateProviderAsync(AppClients clients, string currentProvider)
    {
        Console.WriteLine("(1) Gemini  (2) Groq  (3) Gemma (local Ollama)");
        Console.Write("Select provider: ");
        var provider = Console.ReadLine()?.Trim() switch
        {
            MenuChoices.One => LlmProviders.Gemini,
            MenuChoices.Two => LlmProviders.Groq,
            MenuChoices.Three => LlmProviders.Gemma,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(provider))
        {
            ConsoleHelper.PrintError("Invalid provider.");
            return;
        }

        if (provider.Equals(currentProvider, StringComparison.OrdinalIgnoreCase))
        {
            ConsoleHelper.PrintError("Selected provider is already active.");
            return;
        }

        string apiKey;
        if (provider.Equals(LlmProviders.Gemma, StringComparison.OrdinalIgnoreCase))
        {
            Console.Write("New LLM API Key (press Enter for empty Gemma/Ollama key): ");
            apiKey = ReadMaskedInput();
        }
        else
        {
            Console.Write("New LLM API Key (required when changing provider): ");
            apiKey = ReadMaskedInput();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ConsoleHelper.PrintError("API key is required when changing the LLM provider.");
                return;
            }
        }

        await clients.Admin.UpdateSystemConfigAsync(new UpdateSystemConfigRequest
        {
            LlmProvider = provider,
            LlmApiKey = apiKey
        });
        ConsoleHelper.PrintSuccess("LLM provider and API key updated.");
    }

    private static async Task UpdateSchedulerAsync(AppClients clients)
    {
        Console.Write("Scheduler interval (hours): ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var hours) || hours <= 0)
        {
            ConsoleHelper.PrintError("Invalid interval.");
            return;
        }

        await clients.Admin.UpdateSystemConfigAsync(new UpdateSystemConfigRequest { SchedulerIntervalHours = hours });
        ConsoleHelper.PrintSuccess("Scheduler interval updated.");
    }

    private static async Task UpdateMaxHoursAsync(AppClients clients)
    {
        Console.Write("Max weekly hours: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var hours) || hours <= 0)
        {
            ConsoleHelper.PrintError("Invalid hours.");
            return;
        }

        await clients.Admin.UpdateSystemConfigAsync(new UpdateSystemConfigRequest { MaxWeeklyHours = hours });
        ConsoleHelper.PrintSuccess("Max weekly hours updated.");
    }

    private static async Task UpdateTimesheetDeadlineAsync(AppClients clients, int currentValue)
    {
        Console.Write($"Working days after week end for submission deadline [{currentValue}]: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var days) || days < 0)
        {
            ConsoleHelper.PrintError("Invalid value. Use 0 or a positive number.");
            return;
        }

        await clients.Admin.UpdateSystemConfigAsync(
            new UpdateSystemConfigRequest { TimesheetDeadlineWorkingDaysAfterWeekEnd = days });
        ConsoleHelper.PrintSuccess("Timesheet deadline offset updated.");
    }

    private static string ReadMaskedInput()
    {
        var value = string.Empty;
        ConsoleKeyInfo key;
        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace && value.Length > 0)
            {
                value = value[..^1];
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                value += key.KeyChar;
                Console.Write('*');
            }
        }

        Console.WriteLine();
        return value;
    }
}
