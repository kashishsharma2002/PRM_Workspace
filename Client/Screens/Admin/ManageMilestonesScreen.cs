using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ManageMilestonesScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            ConsoleHelper.PrintHeader("Milestones");

            Console.Write("Enter Project ID: ");
            if (!long.TryParse(Console.ReadLine()?.Trim(), out var projectId))
            {
                ConsoleHelper.PrintError("Invalid project ID.");
                return;
            }

            while (true)
            {
                var list = await clients.Admin.GetMilestonesAsync(projectId);
                if (!ApiLoadHelper.RequireLoaded(list, "Project not found."))
                    return;

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
                    case MenuChoices.One:
                        await AddMilestoneAsync(clients, projectId, list);
                        break;
                    case MenuChoices.Two:
                        await UpdateStatusAsync(clients, projectId, list.Milestones);
                        break;
                    case MenuChoices.Three:
                    case MenuChoices.Exit:
                        return;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            }
        });

    private static async Task AddMilestoneAsync(AppClients clients, long projectId, MilestoneListResponse project)
    {
        var title = FormInputHelper.PromptRequired("Milestone Title");
        var dueDate = FormInputHelper.PromptDateInRange(
            project.StartDate,
            project.EndDate,
            "Due Date",
            allowPastDates: false);
        var points = FormInputHelper.PromptInt("Story Points", minValue: 0);

        await clients.Admin.CreateMilestoneAsync(projectId, new CreateMilestoneRequest
        {
            MilestoneTitle = title,
            DueDate = dueDate,
            StoryPoints = points
        });

        ConsoleHelper.PrintSuccess("Milestone added.");
    }

    private static async Task UpdateStatusAsync(AppClients clients, long projectId, List<MilestoneListItem> milestones)
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
            MenuChoices.One => "NOT_STARTED",
            MenuChoices.Two => "IN_PROGRESS",
            MenuChoices.Three => "DONE",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(status))
        {
            ConsoleHelper.PrintError("Invalid status.");
            return;
        }

        var milestone = milestones[index - 1];
        await clients.Admin.UpdateMilestoneStatusAsync(
            projectId,
            milestone.Id,
            new UpdateMilestoneStatusRequest { MilestoneStatus = status });

        ConsoleHelper.PrintSuccess("Milestone updated.");
    }
}
