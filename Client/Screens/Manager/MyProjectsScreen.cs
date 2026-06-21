using Client.Common;
using Client.Helpers;
using Client.HttpClients;
using Client.Models.Ai;
using Client.Models.ManagerProjects;

namespace Client.Screens.Manager;

public static class MyProjectsScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(() => RunCoreAsync(clients));

    private static async Task RunCoreAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("My Projects");
        var response = await clients.Manager.GetMyProjectsAsync();
        if (response is null || response.Projects.Count == 0)
        {
            Console.WriteLine("No projects found.");
            ConsoleHelper.PrintDivider();
            Console.WriteLine("Press any key to go back...");
            Console.ReadKey(intercept: true);
            return;
        }

        for (var i = 0; i < response.Projects.Count; i++)
        {
            var project = response.Projects[i];
            Console.WriteLine(
                $"{i + 1,2}.  {project.ProjectName,-18}" +
                $"{DateInputHelper.FormatDisplay(project.EndDate),-12}" +
                $"{ConsoleHelper.MapHealthLabel(project.HealthStatus)}");
        }

        ConsoleHelper.PrintDivider();
        Console.Write("Select project number (0 to go back): ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var selection) || selection == 0)
            return;

        if (selection < 1 || selection > response.Projects.Count)
        {
            ConsoleHelper.PrintError("Invalid selection.");
            return;
        }

        var selected = response.Projects[selection - 1];
        await ShowProjectDetailAsync(clients, selected.Id);
    }

    private static async Task ShowProjectDetailAsync(AppClients clients, long projectId)
    {
        var detail = await clients.Manager.GetProjectDetailAsync(projectId);
        if (!ApiLoadHelper.RequireLoaded(detail, "Failed to load project detail."))
            return;

        ConsoleHelper.PrintDivider();
        Console.WriteLine($"-- {detail.ProjectName} --");
        Console.WriteLine($"Health Status : {ConsoleHelper.MapHealthLabel(detail.HealthStatus)}");

        if (detail.RiskFlags.Count > 0)
        {
            Console.WriteLine("Risk Flags:");
            foreach (var flag in detail.RiskFlags)
                Console.WriteLine($"  !  {FormatRiskFlag(flag, detail)}");
        }

        Console.WriteLine("Milestones:");
        Console.WriteLine($"  {"#",-4}{"Title",-20}{"Due Date",-12}{"Status"}");
        for (var i = 0; i < detail.Milestones.Count; i++)
        {
            var m = detail.Milestones[i];
            var overdue = m.IsOverdue ? " OVERDUE" : string.Empty;
            Console.WriteLine(
                $"  {i + 1,-4}{m.MilestoneTitle,-20}" +
                $"{DateInputHelper.FormatDisplay(m.DueDate),-12}" +
                $"{m.MilestoneStatus}{overdue}");
        }

        Console.WriteLine("Allocated Resources:");
        Console.WriteLine($"  {"Name",-18}{"%",-6}{"From",-12}{"To"}");
        foreach (var resource in detail.AllocatedResources)
        {
            Console.WriteLine(
                $"  {resource.EmployeeName,-18}" +
                $"{resource.AllocationPercentage,4:0.#}%  " +
                $"{DateInputHelper.FormatDisplay(resource.AllocationStartDate),-12}" +
                $"{DateInputHelper.FormatDisplay(resource.AllocationEndDate)}");
        }

        ConsoleHelper.PrintDivider();
        Console.Write("[A] Get AI Risk Summary  [B] Back — choice: ");
        var choice = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (choice == MenuChoices.Back)
            return;

        if (choice == MenuChoices.All)
        {
            Console.WriteLine("\nGenerating AI summary...");
            await ScreenRunner.RunSafeAsync(async () =>
            {
                var response = await clients.Ai.GetProjectRiskSummaryAsync(projectId);
                if (response is not null)
                {
                    ConsoleHelper.PrintDivider();
                    Console.WriteLine($"-- AI Risk Summary --");
                    Console.WriteLine(response.Summary);

                    if (response.Recommendations.Count > 0)
                    {
                        Console.WriteLine("\nRecommendations:");
                        foreach (var rec in response.Recommendations)
                        {
                            Console.WriteLine($"  - {rec}");
                        }
                    }
                }
                else
                {
                    ConsoleHelper.PrintError("Failed to retrieve AI risk summary.");
                }
            });

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(intercept: true);
        }
    }

    private static string FormatRiskFlag(string flag, ManagerProjectDetail detail) =>
        flag switch
        {
            "OVERDUE_MILESTONE" => "One or more milestones are overdue",
            "LOW_HOURS" => "Logged hours below expected for one or more resources",
            _ => flag
        };
}
