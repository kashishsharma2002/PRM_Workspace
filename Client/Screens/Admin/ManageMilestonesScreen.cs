using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ManageMilestonesScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Milestones");

        Console.Write("Enter Project ID: ");
        if (!long.TryParse(Console.ReadLine()?.Trim(), out var projectId))
        {
            ConsoleHelper.PrintError("Invalid project ID.");
            return;
        }

        try
        {
            while (true)
            {
                var list = await client.GetAsync<MilestoneListResponse>($"/api/projects/{projectId}/milestones", requireAuth: true);
                if (list is null)
                {
                    ConsoleHelper.PrintError("Project not found.");
                    return;
                }

                ConsoleHelper.PrintHeader($"Milestones — {list.ProjectName}");
                Console.WriteLine($"{"#",-4}{"Title",-22}{"Due Date",-12}{"Story Pts",-10}{"Status"}");
                ConsoleHelper.PrintDivider();

                for (var i = 0; i < list.Milestones.Count; i++)
                {
                    var m = list.Milestones[i];
                    Console.WriteLine($"{i + 1,-4}{m.MilestoneTitle,-22}{DateInputHelper.FormatDisplay(m.DueDate),-12}{m.StoryPoints,-10}{m.MilestoneStatus}");
                }

                ConsoleHelper.PrintDivider();
                Console.WriteLine($"Total: {list.TotalStoryPoints} SP   |   Completed: {list.CompletedStoryPoints} SP   |   Remaining: {list.RemainingStoryPoints} SP");
                ConsoleHelper.PrintDivider();
                Console.WriteLine("1. Add Milestone");
                Console.WriteLine("2. Update Milestone Status");
                Console.WriteLine("3. Back");
                Console.Write("Enter option: ");
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        await AddMilestoneAsync(client, projectId);
                        break;
                    case "2":
                        await UpdateStatusAsync(client, projectId, list.Milestones);
                        break;
                    case "3":
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

    private static async Task AddMilestoneAsync(RestClient client, long projectId)
    {
        Console.Write("Milestone Title  : ");
        var title = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("Due Date         : (DD-MM-YYYY) ");
        if (!DateInputHelper.TryParseToIso(Console.ReadLine() ?? string.Empty, out var dueIso))
        {
            ConsoleHelper.PrintError("Invalid due date.");
            return;
        }

        Console.Write("Story Points     : ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var points) || points < 0)
        {
            ConsoleHelper.PrintError("Invalid story points.");
            return;
        }

        await client.PostAsync<object>($"/api/projects/{projectId}/milestones", new CreateMilestoneRequest
        {
            MilestoneTitle = title,
            DueDate = DateOnly.Parse(dueIso),
            StoryPoints = points
        }, requireAuth: true);

        ConsoleHelper.PrintSuccess("Milestone added.");
    }

    private static async Task UpdateStatusAsync(RestClient client, long projectId, List<MilestoneListItem> milestones)
    {
        if (milestones.Count == 0)
        {
            ConsoleHelper.PrintError("No milestones to update.");
            return;
        }

        Console.Write("Enter Milestone # : ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var index) || index < 1 || index > milestones.Count)
        {
            ConsoleHelper.PrintError("Invalid milestone number.");
            return;
        }

        Console.WriteLine("New Status        : (1) NOT_STARTED  (2) IN_PROGRESS  (3) DONE");
        Console.Write("Enter choice      : ");
        var status = Console.ReadLine()?.Trim() switch
        {
            "1" => "NOT_STARTED",
            "2" => "IN_PROGRESS",
            "3" => "DONE",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(status))
        {
            ConsoleHelper.PrintError("Invalid status.");
            return;
        }

        var milestone = milestones[index - 1];
        await client.PutAsync<object>(
            $"/api/projects/{projectId}/milestones/{milestone.Id}",
            new UpdateMilestoneStatusRequest { MilestoneStatus = status },
            requireAuth: true);

        ConsoleHelper.PrintSuccess("Milestone updated.");
    }
}
