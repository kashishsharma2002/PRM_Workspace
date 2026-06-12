using Client.Helpers;
using Client.HttpClients;
using Client.Models.Ai;

namespace Client.Screens.Manager;

public static class TeamBuilderScreen
{
    private const string BankingPortalExample =
        "For a new banking portal we need a Senior Java Developer with advanced Java and intermediate Spring, " +
        "a DevOps Engineer with intermediate Docker and beginner Kubernetes, " +
        "and a QA Tester with intermediate manual testing and beginner Selenium.";

    public static async Task RunAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("Team Builder with Skill Match");
        Console.WriteLine("\nDescribe your team requirement in plain English");
        Console.WriteLine("(include every role, skills, and proficiency):");
        Console.WriteLine($"  [{ExampleKey}] Load banking portal example");
        Console.Write("> ");

        var requirement = ReadRequirement();
        if (string.IsNullOrWhiteSpace(requirement))
        {
            ConsoleHelper.PrintError("Requirement description cannot be empty.");
            return;
        }

        Console.WriteLine("\nSearching... (calling AI)");

        var response = await clients.Ai.BuildTeamAsync(requirement);
        if (response is null || response.Roles.Count == 0)
        {
            ConsoleHelper.PrintError("No AI team builder results found or server error occurred.");
            return;
        }

        DisplayResults(response);
        ConsoleHelper.PrintDivider();
        Console.Write("Press any key to go back...");
        Console.ReadKey(intercept: true);
    }

    private const string ExampleKey = "E";

    private static string ReadRequirement()
    {
        var firstLine = Console.ReadLine()?.Trim();
        if (string.Equals(firstLine, ExampleKey, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"\n  Example loaded:\n  \"{BankingPortalExample}\"\n");
            return BankingPortalExample;
        }

        return firstLine ?? string.Empty;
    }

    private static void DisplayResults(TeamBuilderResponse response)
    {
        ConsoleHelper.PrintDivider();
        Console.WriteLine("Results:");
        Console.WriteLine();

        foreach (var role in response.Roles)
        {
            Console.WriteLine($"Role: {role.RoleTitle}");
            Console.WriteLine($"  Required: {FormatRequiredSkills(role.RequiredSkills)}");
            Console.WriteLine($"  Status: {role.Status}");

            if (string.Equals(role.Status, "FILLED", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  Match:  {role.AssignedEmployeeName} (score {role.MatchScore})");
                if (!string.IsNullOrWhiteSpace(role.Reason))
                    Console.WriteLine($"  Reason: {role.Reason}");
            }
            else if (role.Gap is not null)
            {
                Console.WriteLine($"  Why:   {FormatGapReason(role.Gap.ReasonType)}");
                Console.WriteLine($"  Note:  {role.Gap.Message}");
                if (!string.IsNullOrWhiteSpace(role.Gap.AlternativeEmployeeName)
                    && !string.IsNullOrWhiteSpace(role.Gap.AvailableFromDate))
                {
                    Console.WriteLine(
                        $"         {role.Gap.AlternativeEmployeeName} may be available from {role.Gap.AvailableFromDate}.");
                }
            }

            Console.WriteLine();
        }

        Console.WriteLine("  Note: AI-generated suggestions. No allocation performed.");
    }

    private static string FormatGapReason(string reasonType) =>
        reasonType switch
        {
            "NO_SKILL" => "NO_SKILL — hire or train",
            "ALLOCATED_ELSEWHERE" => "ALLOCATED_ELSEWHERE — not fully available",
            "ALREADY_ASSIGNED_IN_TEAM" => "ALREADY_ASSIGNED_IN_TEAM — person used in another role",
            _ => reasonType
        };

    private static string FormatRequiredSkills(IReadOnlyList<TeamBuilderSkillRequirement> skills)
    {
        if (skills.Count == 0)
            return "(none inferred)";

        return string.Join(", ", skills.Select(s => $"{s.SkillName} ({s.MinProficiency})"));
    }
}
