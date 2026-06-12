using System.Text.RegularExpressions;
using Client.Helpers;
using Client.HttpClients;
using Client.Models.ManagerProjects;

namespace Client.Screens.Manager;

internal static class ManagerProjectPicker
{
    private static readonly Regex NameWithIdPattern = new(
        @"^(?<name>.+?)\s*\((?<id>\d+)\)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static async Task<ManagerProjectListItem?> PromptByNameOrIdAsync(
        AppClients clients,
        string stepLabel = "Step 1 — Select Project")
    {
        var projectsResponse = await clients.Manager.GetMyProjectsAsync();
        if (projectsResponse is null || projectsResponse.Projects.Count == 0)
        {
            Console.WriteLine("No projects found.");
            return null;
        }

        var projects = projectsResponse.Projects;

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine(stepLabel);
            Console.Write("Enter project name or ID (enter 0 to list projects, B to go back): ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                ConsoleHelper.PrintError("Project name or ID is required.");
                continue;
            }

            if (string.Equals(input, "B", StringComparison.OrdinalIgnoreCase))
                return null;

            if (input == "0")
            {
                DisplayProjectList(projects);
                continue;
            }

            var match = ResolveProject(input, projects);
            if (match is not null)
                return match;

            ConsoleHelper.PrintError("Project not found. Enter a valid project name or ID from your project list.");
        }
    }

    private static void DisplayProjectList(IReadOnlyList<ManagerProjectListItem> projects)
    {
        Console.WriteLine();
        Console.WriteLine($"{"Project",-28}{"ID",-8}{"Health"}");
        ConsoleHelper.PrintDivider();

        foreach (var project in projects)
        {
            Console.WriteLine(
                $"{project.ProjectName,-28}" +
                $"{project.Id,-8}" +
                $"{ConsoleHelper.MapHealthLabel(project.HealthStatus)}");
        }

        ConsoleHelper.PrintDivider();
    }

    private static ManagerProjectListItem? ResolveProject(string input, IReadOnlyList<ManagerProjectListItem> projects)
    {
        if (long.TryParse(input, out var projectId))
            return projects.FirstOrDefault(p => p.Id == projectId);

        var nameWithId = NameWithIdPattern.Match(input);
        if (nameWithId.Success && long.TryParse(nameWithId.Groups["id"].Value, out var parsedId))
            return projects.FirstOrDefault(p => p.Id == parsedId);

        var exactNameMatches = projects
            .Where(p => string.Equals(p.ProjectName, input, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (exactNameMatches.Count == 1)
            return exactNameMatches[0];

        if (exactNameMatches.Count > 1)
            return null;

        var partialMatches = projects
            .Where(p => p.ProjectName.Contains(input, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return partialMatches.Count == 1 ? partialMatches[0] : null;
    }
}
