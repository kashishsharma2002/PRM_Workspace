using Client.Helpers;
using Client.HttpClients;
using Client.Models.Allocations;
using Client.Models.ManagerProjects;
using Client.Models.Ai;

namespace Client.Screens.Manager;

public static class AllocateResourceScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Allocate Resource");
            Console.WriteLine("1. Find employee/resource using AI");
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
                        await RunAiAllocationFlowAsync(clients);
                        break;
                    case "2":
                        await RunDirectAllocationAsync(clients);
                        break;
                    case "3":
                        await RunEndAllocationAsync(clients);
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

    private static async Task RunDirectAllocationAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Direct Allocation");
            var projects = await clients.Manager.GetMyProjectsAsync();
            if (projects is null || projects.Projects.Count == 0)
            {
                Console.WriteLine("No projects found.");
                return;
            }

            var allocatableProjects = projects.Projects
                .Where(p => p.ProjectStatus.Equals("PLANNED", StringComparison.OrdinalIgnoreCase) ||
                            p.ProjectStatus.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (allocatableProjects.Count == 0)
            {
                Console.WriteLine("No projects available for allocation. (Only PLANNED and ACTIVE projects can receive allocations.)");
                return;
            }

            for (var i = 0; i < allocatableProjects.Count; i++)
            {
                var project = allocatableProjects[i];
                Console.WriteLine($"{i + 1,2}.  {project.ProjectName} ({project.ProjectStatus}) ({project.Id})");
            }

            ConsoleHelper.PrintDivider();
            Console.Write("Select project number (B to go back): ");
            var projectInput = Console.ReadLine()?.Trim();
            if (string.Equals(projectInput, "B", StringComparison.OrdinalIgnoreCase))
                return;

            if (!int.TryParse(projectInput, out var projectSelection)
                || projectSelection < 1
                || projectSelection > allocatableProjects.Count)
            {
                ConsoleHelper.PrintError("Invalid project selection.");
                continue;
            }

            var selectedProject = allocatableProjects[projectSelection - 1];

            var employeeId = await ManagerTeamEmployeePicker.PromptEmployeeIdAsync(clients);
            if (employeeId is null)
                return;

            var percentage = FormInputHelper.PromptPercentage("Utilisation %");

            var startDate = FormInputHelper.PromptDate("From Date");
            var endDate = FormInputHelper.PromptDate("To Date");

            while (endDate <= startDate)
            {
                ConsoleHelper.PrintError("End date must be after start date.");
                endDate = FormInputHelper.PromptDate("To Date");
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
                var result = await clients.Manager.CreateAllocationAsync(new CreateAllocationRequest
                {
                    EmployeeId = employeeId.Value,
                    ProjectId = selectedProject.Id,
                    AllocationPercentage = percentage,
                    AllocationStartDate = startDate,
                    AllocationEndDate = endDate
                });

                if (result is not null)
                {
                    ConsoleHelper.PrintSuccess(
                        $"Allocation saved. Employee/Resource {result.EmployeeId} → Project {result.ProjectId} " +
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

    private static async Task RunEndAllocationAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("End Allocation");
        var projects = await clients.Manager.GetMyProjectsAsync();
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
        var detail = await clients.Manager.GetProjectDetailAsync(selectedProject.Id);

        if (detail is null || detail.AllocatedResources.Count == 0)
        {
            Console.WriteLine("No active allocations on this project.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Active Allocations on this project:");
        Console.WriteLine($"  {"#",-4}{"Employee/Resource",-24}{"%",-6}{"From",-14}To");
        for (var i = 0; i < detail.AllocatedResources.Count; i++)
        {
            var resource = detail.AllocatedResources[i];
            Console.WriteLine(
                $"  {i + 1,2}.  {resource.EmployeeName,-24}" +
                $"{resource.AllocationPercentage,4:0.#}%  " +
                $"{DateInputHelper.FormatDisplay(resource.AllocationStartDate),-14}" +
                $"{DateInputHelper.FormatDisplay(resource.AllocationEndDate)}");
        }

        ConsoleHelper.PrintDivider();
        Console.Write("Select allocation/resource to end: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var allocationSelection)
            || allocationSelection < 1
            || allocationSelection > detail.AllocatedResources.Count)
        {
            ConsoleHelper.PrintError("Invalid selection.");
            return;
        }

        var selected = detail.AllocatedResources[allocationSelection - 1];
        Console.WriteLine();
        Console.Write($"End {selected.EmployeeName}'s allocation/resource on {detail.ProjectName}? [Y/N]: ");
        var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (confirm != "Y")
            return;

        var result = await clients.Manager.EndAllocationAsync(selected.AllocationId);

        if (result is not null)
        {
            ConsoleHelper.PrintSuccess(
                $"Allocation ended. {selected.EmployeeName} (Employee/Resource) freed from {detail.ProjectName} as of " +
                $"{DateInputHelper.FormatDisplay(result.AllocationEndDate)}.");
        }
    }

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
            ConsoleHelper.PrintError("No AI matches found or server error occurred.");
            return;
        }

        ConsoleHelper.PrintDivider();
        Console.WriteLine("AI-MATCHED RESULTS");
        ConsoleHelper.PrintDivider();

        for (var i = 0; i < response.Matches.Count; i++)
        {
            var match = response.Matches[i];
            Console.WriteLine($"{i + 1}.  {match.EmployeeName,-24} (Score: {match.MatchScore}%)");
            Console.WriteLine($"    Matched Skill: {match.SkillName}");
            if (!string.IsNullOrWhiteSpace(match.Reason))
            {
                Console.WriteLine($"    Explanation  : {match.Reason}");
            }
            Console.WriteLine();
        }

        ConsoleHelper.PrintDivider();
        ConsoleHelper.PrintSuccess("AI-matched employees/resources retrieved. Go to Allocate Resources to allocate them to the project.");
        Console.WriteLine("\nPress any key to go back...");
        Console.ReadKey(intercept: true);
    }
}
