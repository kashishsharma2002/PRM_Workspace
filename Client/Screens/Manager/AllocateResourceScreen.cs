using Client.Helpers;
using Client.HttpClients;
using Client.Models.Allocations;
using Client.Models.ManagerProjects;
using Client.Models.Ai;

namespace Client.Screens.Manager;

public static class AllocateResourceScreen
{
    public static async Task RunAsync(RestClient client)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Allocate Resource");
            Console.WriteLine("1. Find resource using AI");
            Console.WriteLine("2. Allocate directly (I already know who I want)");
            Console.WriteLine("3. End an existing allocation");
            Console.WriteLine("4. Back");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await RunAiAllocationFlowAsync(client);
                        break;
                    case "2":
                        await RunDirectAllocationAsync(client);
                        break;
                    case "3":
                        await RunEndAllocationAsync(client);
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

    private static async Task RunDirectAllocationAsync(RestClient client)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Direct Allocation");
            var projects = await client.GetAsync<ManagerProjectListResponse>("/api/projects/my", requireAuth: true);
            if (projects is null || projects.Projects.Count == 0)
            {
                Console.WriteLine("No projects found.");
                return;
            }

            for (var i = 0; i < projects.Projects.Count; i++)
            {
                var project = projects.Projects[i];
                Console.WriteLine($"{i + 1,2}.  {project.ProjectName} ({project.Id})");
            }

            ConsoleHelper.PrintDivider();
            Console.Write("Select project number (B to go back): ");
            var projectInput = Console.ReadLine()?.Trim();
            if (string.Equals(projectInput, "B", StringComparison.OrdinalIgnoreCase))
                return;

            if (!int.TryParse(projectInput, out var projectSelection)
                || projectSelection < 1
                || projectSelection > projects.Projects.Count)
            {
                ConsoleHelper.PrintError("Invalid project selection.");
                continue;
            }

            var selectedProject = projects.Projects[projectSelection - 1];

            var employeeId = await ManagerTeamEmployeePicker.PromptEmployeeIdAsync(client);
            if (employeeId is null)
                return;

            Console.Write("Utilisation % (1-100): ");
            if (!decimal.TryParse(Console.ReadLine()?.Trim(), out var percentage) || percentage < 1 || percentage > 100)
            {
                ConsoleHelper.PrintError("Allocation percentage must be between 1 and 100.");
                continue;
            }

            Console.Write("From Date (DD-MM-YYYY): ");
            if (!DateInputHelper.TryParseToIso(Console.ReadLine() ?? string.Empty, out var startIso))
            {
                ConsoleHelper.PrintError("Invalid start date.");
                continue;
            }

            Console.Write("To Date (DD-MM-YYYY): ");
            if (!DateInputHelper.TryParseToIso(Console.ReadLine() ?? string.Empty, out var endIso))
            {
                ConsoleHelper.PrintError("Invalid end date.");
                continue;
            }

            var startDate = DateOnly.Parse(startIso);
            var endDate = DateOnly.Parse(endIso);
            if (endDate <= startDate)
            {
                ConsoleHelper.PrintError("End date must be after start date.");
                continue;
            }

            ConsoleHelper.PrintDivider();
            Console.Write("[C] Confirm Allocation     [B] Back: ");
            var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (confirm == "B")
                return;
            if (confirm != "C")
            {
                ConsoleHelper.PrintError("Invalid choice. Enter C to confirm or B to go back.");
                continue;
            }

            try
            {
                var result = await client.PostAsync<CreateAllocationResponse>("/api/allocations", new CreateAllocationRequest
                {
                    EmployeeId = employeeId.Value,
                    ProjectId = selectedProject.Id,
                    AllocationPercentage = percentage,
                    AllocationStartDate = startDate,
                    AllocationEndDate = endDate
                }, requireAuth: true);

                if (result is not null)
                {
                    ConsoleHelper.PrintSuccess(
                        $"Allocation saved. Employee {result.EmployeeId} → Project {result.ProjectId} " +
                        $"({result.AllocationPercentage:0.#}%, {DateInputHelper.FormatDisplay(startDate)}–{DateInputHelper.FormatDisplay(endDate)})");
                    return;
                }

                ConsoleHelper.PrintError("Allocation could not be saved. Please try again.");
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

    private static async Task RunEndAllocationAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("End Allocation");
        var projects = await client.GetAsync<ManagerProjectListResponse>("/api/projects/my", requireAuth: true);
        if (projects is null || projects.Projects.Count == 0)
        {
            Console.WriteLine("No projects found.");
            return;
        }

        for (var i = 0; i < projects.Projects.Count; i++)
        {
            var project = projects.Projects[i];
            Console.WriteLine($"{i + 1,2}.  {project.ProjectName} ({project.Id})");
        }

        ConsoleHelper.PrintDivider();
        Console.Write("Select project number: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var projectSelection)
            || projectSelection < 1
            || projectSelection > projects.Projects.Count)
        {
            ConsoleHelper.PrintError("Invalid project selection.");
            return;
        }

        var selectedProject = projects.Projects[projectSelection - 1];
        var detail = await client.GetAsync<ManagerProjectDetail>(
            $"/api/projects/{selectedProject.Id}/manager",
            requireAuth: true);

        if (detail is null || detail.AllocatedResources.Count == 0)
        {
            Console.WriteLine("No active allocations on this project.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Active Allocations on this project:");
        Console.WriteLine($"  {"#",-4}{"Employee",-16}{"%",-6}{"From",-14}To");
        for (var i = 0; i < detail.AllocatedResources.Count; i++)
        {
            var resource = detail.AllocatedResources[i];
            Console.WriteLine(
                $"  {i + 1,2}.  {resource.EmployeeName,-14}" +
                $"{resource.AllocationPercentage,4:0.#}%  " +
                $"{DateInputHelper.FormatDisplay(resource.AllocationStartDate),-14}" +
                $"{DateInputHelper.FormatDisplay(resource.AllocationEndDate)}");
        }

        ConsoleHelper.PrintDivider();
        Console.Write("Select allocation to end: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var allocationSelection)
            || allocationSelection < 1
            || allocationSelection > detail.AllocatedResources.Count)
        {
            ConsoleHelper.PrintError("Invalid selection.");
            return;
        }

        var selected = detail.AllocatedResources[allocationSelection - 1];
        Console.WriteLine();
        Console.Write($"End {selected.EmployeeName}'s allocation on {detail.ProjectName}? [Y/N]: ");
        var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (confirm != "Y")
            return;

        var result = await client.PutAsync<EndAllocationResponse>(
            $"/api/allocations/{selected.AllocationId}/end",
            new { },
            requireAuth: true);

        if (result is not null)
        {
            ConsoleHelper.PrintSuccess(
                $"Allocation ended. {selected.EmployeeName} freed from {detail.ProjectName} as of " +
                $"{DateInputHelper.FormatDisplay(result.AllocationEndDate)}.");
        }
    }

    private static async Task RunAiAllocationFlowAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Allocate Resource");

        var selectedProject = await ManagerProjectPicker.PromptByNameOrIdAsync(client);
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
        var url = $"/api/ai/projects/{selectedProject.Id}/skill-match?requirement={Uri.EscapeDataString(requirement)}";
        var response = await client.GetAsync<AiSkillMatchResponse>(url, requireAuth: true);
        if (response is null || response.Matches.Count == 0)
        {
            ConsoleHelper.PrintError("No AI matches found or server error occurred.");
            return;
        }

        ConsoleHelper.PrintDivider();
        Console.WriteLine("AI-MATCHED RESULTS");
        ConsoleHelper.PrintDivider();

        for (var i = 0; i < response.Matches.Count; i++)
        {
            var match = response.Matches[i];
            Console.WriteLine($"{i + 1}.  {match.EmployeeName,-20} (Score: {match.MatchScore}%)");
            Console.WriteLine($"    Matched Skill: {match.SkillName}");
            if (!string.IsNullOrWhiteSpace(match.Reason))
            {
                Console.WriteLine($"    Explanation  : {match.Reason}");
            }
            Console.WriteLine();
        }

        ConsoleHelper.PrintDivider();
        ConsoleHelper.PrintSuccess("AI-matched resources retrieved. Actual resource allocation is held per pending requirements.");
        Console.WriteLine("\nPress any key to go back...");
        Console.ReadKey(intercept: true);
    }
}
