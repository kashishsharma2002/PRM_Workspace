using Client.Helpers;
using Client.HttpClients;
using Client.Models.Ai;
using Client.Models.ManagerProjects;

namespace Client.Screens.Manager;

public static class AiAssistantScreen
{
    public static async Task RunAsync(RestClient client)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("AI Assistant");
            Console.WriteLine("1. Skill Match    — Find best employees for a project requirement");
            Console.WriteLine("2. Risk Summary   — Get a health analysis for a project");
            Console.WriteLine("3. Team Builder   — Build a whole team with skill match in one search");
            Console.WriteLine("4. Back");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await RunSkillMatchAsync(client);
                        break;
                    case "2":
                        await RunRiskSummaryAsync(client);
                        break;
                    case "3":
                        await TeamBuilderScreen.RunAsync(client);
                        break;
                    case "4":
                        return;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            }
            catch (SessionExpiredException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorDisplayHelper.HandleException(ex);
            }
        }
    }

    private static async Task RunSkillMatchAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Skill Match");
        Console.WriteLine("\nDescribe your project requirement in plain English:");
        Console.Write("> ");
        var requirement = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(requirement))
        {
            ConsoleHelper.PrintError("Requirement description cannot be empty.");
            return;
        }

        Console.WriteLine("\nSearching... (calling AI)");
        var url = $"/api/ai/skill-match?requirement={Uri.EscapeDataString(requirement)}";

        var response = await client.GetAsync<AiSkillMatchResponse>(url, requireAuth: true);
        if (response is null || response.Matches.Count == 0)
        {
            ConsoleHelper.PrintError("No AI matches found or server error occurred.");
            return;
        }

        ConsoleHelper.PrintDivider();
        Console.WriteLine("Results:");
        for (var i = 0; i < response.Matches.Count; i++)
        {
            var match = response.Matches[i];
            Console.WriteLine($"  {i + 1}.  {match.EmployeeName}");
            var reason = !string.IsNullOrWhiteSpace(match.Reason)
                ? match.Reason
                : $"{match.SkillName} match (score {match.MatchScore}%).";
            Console.WriteLine($"      Reason: {reason}");
            Console.WriteLine();
        }

        Console.WriteLine("  Note: These are AI-generated suggestions. Always verify availability");
        Console.WriteLine("  and skills with the employee before allocating.");
        ConsoleHelper.PrintDivider();
        Console.Write("[A] Go to Allocate Resource     [B] Back — choice: ");
        var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

        if (choice == "A")
            await AllocateResourceScreen.RunAsync(client);
    }

    private static async Task RunRiskSummaryAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Risk Summary");

        var projectsResponse = await client.GetAsync<ManagerProjectListResponse>("/api/projects/my", requireAuth: true);
        if (projectsResponse is null || projectsResponse.Projects.Count == 0)
        {
            Console.WriteLine("No projects found.");
            return;
        }

        Console.WriteLine("\nSelect project:");
        for (var i = 0; i < projectsResponse.Projects.Count; i++)
        {
            var p = projectsResponse.Projects[i];
            Console.WriteLine($"  {i + 1}.  {p.ProjectName,-20} {ConsoleHelper.MapHealthLabel(p.HealthStatus)}");
        }

        ConsoleHelper.PrintDivider();
        Console.Write("Enter project number: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var selectNum) || selectNum == 0)
            return;

        if (selectNum < 1 || selectNum > projectsResponse.Projects.Count)
        {
            ConsoleHelper.PrintError("Invalid selection.");
            return;
        }

        var project = projectsResponse.Projects[selectNum - 1];

        Console.WriteLine("\nGenerating AI summary...");
        var url = $"/api/ai/projects/{project.Id}/risk-summary";

        var response = await client.GetAsync<AiRiskSummaryResponse>(url, requireAuth: true);
        if (response is null)
        {
            ConsoleHelper.PrintError("Failed to load AI risk summary.");
            return;
        }

        ConsoleHelper.PrintDivider();
        Console.WriteLine($"\"{response.Summary}\"");
        Console.WriteLine();
        Console.WriteLine("  Note: AI-generated from current milestone and timesheet data.");
        ConsoleHelper.PrintDivider();
        Console.Write("Press any key to go back...");
        Console.ReadKey(intercept: true);
    }
}
