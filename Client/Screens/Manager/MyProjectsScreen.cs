using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Manager;

public static class MyProjectsScreen
{
    public static async Task RunAsync(RestClient client)
    {
        try
        {
            ConsoleHelper.PrintHeader("My Projects");
            var response = await client.GetAsync<ManagerProjectListResponse>("/api/projects/my", requireAuth: true);
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
            await ShowProjectDetailAsync(client, selected.Id);
        }
        catch (SessionExpiredException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError(ex.Message);
        }
    }

    private static async Task ShowProjectDetailAsync(RestClient client, long projectId)
    {
        var detail = await client.GetAsync<ManagerProjectDetail>($"/api/projects/{projectId}/manager", requireAuth: true);
        if (detail is null)
        {
            ConsoleHelper.PrintError("Failed to load project detail.");
            return;
        }

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
        if (choice == "A")
            Console.WriteLine("AI Risk Summary is available in Phase 8.");

        Console.WriteLine("Press any key to go back...");
        Console.ReadKey(intercept: true);
    }

    private static string FormatRiskFlag(string flag, ManagerProjectDetail detail) =>
        flag switch
        {
            "OVERDUE_MILESTONE" => "One or more milestones are overdue",
            "LOW_HOURS" => "Logged hours below expected for one or more resources",
            _ => flag
        };
}
