using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Manager;

public static partial class AllocateResourceScreen
{
    private static async Task RunAiAllocationFlowAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("Allocate Resource");

        var selectedProject = await ManagerProjectPicker.PromptByNameOrIdAsync(clients);
        if (selectedProject is null)
            return;

        Console.WriteLine($"\nSelected project: {selectedProject.ProjectName} ({selectedProject.Id})");
        Console.WriteLine("\nStep 2 — Describe your requirement");
        Console.WriteLine("Type what kind of resource you need:");
        Console.Write("> ");
        var requirement = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(requirement))
        {
            ConsoleHelper.PrintError("Requirement description cannot be empty.");
            return;
        }

        Console.WriteLine("\nSearching... (AI matching in progress)");
        var response = await clients.Ai.GetProjectSkillMatchAsync(selectedProject.Id, requirement);
        if (response is null || response.Matches.Count == 0)
        {
            ConsoleHelper.PrintError("No skill matches found for this requirement.");
            return;
        }

        ConsoleHelper.PrintDivider();
        Console.WriteLine("SKILL MATCHES");
        ConsoleHelper.PrintDivider();

        for (var i = 0; i < response.Matches.Count; i++)
        {
            var match = response.Matches[i];
            Console.WriteLine($"{i + 1}.  {match.EmployeeName,-24} (Score: {match.MatchScore}%)");
            Console.WriteLine($"    Matched Skill: {match.SkillName}");
            if (!string.IsNullOrWhiteSpace(match.Reason))
                Console.WriteLine($"    Explanation  : {match.Reason}");

            Console.WriteLine();
        }

        ConsoleHelper.PrintDivider();
        ConsoleHelper.PrintSuccess("Skill-matched employees/resources retrieved. Go to Allocate Resources to allocate them to the project.");
        Console.WriteLine("\nPress any key to go back...");
        Console.ReadKey(intercept: true);
    }
}
