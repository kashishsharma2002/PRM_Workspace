using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class SystemConfigScreen
{
    public static async Task RunAsync(RestClient client)
    {
        try
        {
            while (true)
            {
                var config = await client.GetAsync<SystemConfigResponse>("/api/system-config", requireAuth: true);
                if (config is null)
                {
                    ConsoleHelper.PrintError("Failed to load configuration.");
                    return;
                }

                ConsoleHelper.PrintHeader("System Configuration");
                Console.WriteLine("Current Settings:");
                Console.WriteLine($"  LLM Provider        :  {config.LlmProvider}");
                Console.WriteLine($"  LLM API Key         :  {(string.IsNullOrEmpty(config.LlmApiKeyMasked) ? "(not set)" : config.LlmApiKeyMasked)}");
                Console.WriteLine($"  Scheduler Interval  :  {config.SchedulerIntervalHours} hours");
                Console.WriteLine($"  Max Weekly Hours    :  {config.MaxWeeklyHours}");
                Console.WriteLine($"  Low Hours Threshold :  {config.HealthLowHoursThreshold:0.##}");
                Console.WriteLine($"  Approaching Deadline:  {config.HealthApproachingDeadlineDays} days");
                ConsoleHelper.PrintDivider();
                Console.WriteLine("1. Update LLM API Key");
                Console.WriteLine("2. Change LLM Provider  (Gemini / Groq)");
                Console.WriteLine("3. Update Scheduler Interval");
                Console.WriteLine("4. Update Max Weekly Hours");
                Console.WriteLine("5. Update Health Thresholds");
                Console.WriteLine("6. Back");
                Console.Write("Enter option: ");
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        await UpdateApiKeyAsync(client);
                        break;
                    case "2":
                        await UpdateProviderAsync(client);
                        break;
                    case "3":
                        await UpdateSchedulerAsync(client);
                        break;
                    case "4":
                        await UpdateMaxHoursAsync(client);
                        break;
                    case "5":
                        await UpdateHealthThresholdsAsync(client);
                        break;
                    case "6":
                    case "0":
                        return;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            }
        }
        catch (SessionExpiredException) { throw; }
        catch (Exception ex) { ErrorDisplayHelper.HandleException(ex); }
    }

    private static async Task UpdateApiKeyAsync(RestClient client)
    {
        Console.Write("New LLM API Key: ");
        var key = ReadMaskedInput();
        if (string.IsNullOrWhiteSpace(key))
        {
            ConsoleHelper.PrintError("API key is required.");
            return;
        }

        await client.PutAsync<object>("/api/system-config", new UpdateSystemConfigRequest { LlmApiKey = key }, requireAuth: true);
        ConsoleHelper.PrintSuccess("LLM API key updated.");
    }

    private static async Task UpdateProviderAsync(RestClient client)
    {
        Console.WriteLine("(1) Gemini  (2) Groq");
        Console.Write("Select provider: ");
        var provider = Console.ReadLine()?.Trim() switch
        {
            "1" => "Gemini",
            "2" => "Groq",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(provider))
        {
            ConsoleHelper.PrintError("Invalid provider.");
            return;
        }

        await client.PutAsync<object>("/api/system-config", new UpdateSystemConfigRequest { LlmProvider = provider }, requireAuth: true);
        ConsoleHelper.PrintSuccess("LLM provider updated.");
    }

    private static async Task UpdateSchedulerAsync(RestClient client)
    {
        Console.Write("Scheduler interval (hours): ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var hours) || hours <= 0)
        {
            ConsoleHelper.PrintError("Invalid interval.");
            return;
        }

        await client.PutAsync<object>("/api/system-config", new UpdateSystemConfigRequest { SchedulerIntervalHours = hours }, requireAuth: true);
        ConsoleHelper.PrintSuccess("Scheduler interval updated.");
    }

    private static async Task UpdateMaxHoursAsync(RestClient client)
    {
        Console.Write("Max weekly hours: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var hours) || hours <= 0)
        {
            ConsoleHelper.PrintError("Invalid hours.");
            return;
        }

        await client.PutAsync<object>("/api/system-config", new UpdateSystemConfigRequest { MaxWeeklyHours = hours }, requireAuth: true);
        ConsoleHelper.PrintSuccess("Max weekly hours updated.");
    }

    private static async Task UpdateHealthThresholdsAsync(RestClient client)
    {
        Console.Write("Low hours threshold (0.01-1, e.g. 0.6): ");
        if (!decimal.TryParse(Console.ReadLine()?.Trim(), out var lowHours) || lowHours is < 0.01m or > 1m)
        {
            ConsoleHelper.PrintError("Invalid low hours threshold.");
            return;
        }

        Console.Write("Approaching deadline days (>0): ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var days) || days <= 0)
        {
            ConsoleHelper.PrintError("Invalid approaching deadline days.");
            return;
        }

        await client.PutAsync<object>(
            "/api/system-config",
            new UpdateSystemConfigRequest
            {
                HealthLowHoursThreshold = lowHours,
                HealthApproachingDeadlineDays = days
            },
            requireAuth: true);
        ConsoleHelper.PrintSuccess("Health thresholds updated.");
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
