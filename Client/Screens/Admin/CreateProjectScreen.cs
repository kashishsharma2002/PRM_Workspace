using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class CreateProjectScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Create Project");

        Console.Write("Project Name        : ");
        var name = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("Description         : ");
        var description = Console.ReadLine()?.Trim();

        Console.Write("Start Date          : (DD-MM-YYYY) ");
        if (!DateInputHelper.TryParseToIso(Console.ReadLine() ?? string.Empty, out var startIso))
        {
            ConsoleHelper.PrintError("Invalid start date.");
            return;
        }

        Console.Write("End Date            : (DD-MM-YYYY) ");
        if (!DateInputHelper.TryParseToIso(Console.ReadLine() ?? string.Empty, out var endIso))
        {
            ConsoleHelper.PrintError("Invalid end date.");
            return;
        }

        Console.WriteLine("Status              : (1) PLANNED  (2) ACTIVE  (3) ON_HOLD");
        Console.Write("Select status [1-3]: ");
        var status = Console.ReadLine()?.Trim() switch
        {
            "1" => "PLANNED",
            "2" => "ACTIVE",
            "3" => "ON_HOLD",
            _ => string.Empty
        };

        Console.Write("Assign Manager      : (Enter Manager User ID) ");
        if (!long.TryParse(Console.ReadLine()?.Trim(), out var managerId))
        {
            ConsoleHelper.PrintError("Invalid manager user ID.");
            return;
        }

        Console.Write("Total Story Points  : ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var storyPoints) || storyPoints < 0)
        {
            ConsoleHelper.PrintError("Invalid story points.");
            return;
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(status))
        {
            ConsoleHelper.PrintError("All required fields must be provided.");
            return;
        }

        ConsoleHelper.PrintDivider();
        Console.Write("[S] Save  [B] Back — choice: ");
        if (Console.ReadLine()?.Trim().ToUpperInvariant() != "S")
            return;

        try
        {
            var result = await client.PostAsync<CreateProjectResponse>("/api/projects", new CreateProjectRequest
            {
                ProjectName = name,
                Description = description,
                StartDate = DateOnly.Parse(startIso),
                EndDate = DateOnly.Parse(endIso),
                ProjectStatus = status,
                ManagerUserId = managerId,
                TotalStoryPoints = storyPoints
            }, requireAuth: true);

            if (result is not null)
                ConsoleHelper.PrintSuccess($"Project created ({result.ProjectCode}).");
        }
        catch (SessionExpiredException) { throw; }
        catch (Exception ex) { ConsoleHelper.PrintError(ex.Message); }
    }
}
