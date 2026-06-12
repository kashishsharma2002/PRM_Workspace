using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class CreateProjectScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("Create Project");

        var name = FormInputHelper.PromptRequired("Project Name");
        Console.Write("Description         : ");
        var description = Console.ReadLine()?.Trim();

        var startDate = FormInputHelper.PromptDateNotBeforeToday("Start Date");
        var endDate = FormInputHelper.PromptDate("End Date");

        while (endDate <= startDate)
        {
            ConsoleHelper.PrintError($"End date must be after start date.");
            endDate = FormInputHelper.PromptDate("End Date");
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

        var managerId = FormInputHelper.PromptId("Assign Manager (User ID)");
        var storyPoints = FormInputHelper.PromptInt("Total Story Points", minValue: 0);

        if (string.IsNullOrWhiteSpace(status))
        {
            ConsoleHelper.PrintError("Status is required.");
            return;
        }

        ConsoleHelper.PrintDivider();
        Console.Write("[S] Save  [B] Back — choice: ");
        if (Console.ReadLine()?.Trim().ToUpperInvariant() != "S")
            return;

        try
        {
            var result = await clients.Admin.CreateProjectAsync(new CreateProjectRequest
            {
                ProjectName = name,
                Description = description,
                StartDate = startDate,
                EndDate = endDate,
                ProjectStatus = status,
                ManagerUserId = managerId,
                TotalStoryPoints = storyPoints
            });

            if (result is not null)
                ConsoleHelper.PrintSuccess($"Project created ({result.ProjectCode}).");
        }
        catch (SessionExpiredException) { throw; }
        catch (Exception ex) { ErrorDisplayHelper.HandleException(ex); }
    }
}
