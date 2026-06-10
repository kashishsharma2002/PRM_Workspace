using Client.Helpers;
using Client.HttpClients;
using Client.Models.Allocations;
using Client.Models.ManagerProjects;

namespace Client.Screens.Manager;

public static class AllocateResourceScreen
{
    public static async Task RunAsync(RestClient client)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Allocate Resource");
            Console.WriteLine("1. Find resource using AI (Phase 8)");
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
                        Console.WriteLine("AI-assisted allocation will be available in Phase 8.");
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
        Console.Write("Select project number: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var projectSelection)
            || projectSelection < 1
            || projectSelection > projects.Projects.Count)
        {
            ConsoleHelper.PrintError("Invalid project selection.");
            return;
        }

        var selectedProject = projects.Projects[projectSelection - 1];

        var employeeId = await ManagerTeamEmployeePicker.PromptEmployeeIdAsync(client);
        if (employeeId is null)
            return;

        Console.Write("Utilisation % (1-100): ");
        if (!decimal.TryParse(Console.ReadLine()?.Trim(), out var percentage) || percentage < 1 || percentage > 100)
        {
            ConsoleHelper.PrintError("Allocation percentage must be between 1 and 100.");
            return;
        }

        Console.Write("From Date (DD-MM-YYYY): ");
        if (!DateInputHelper.TryParseToIso(Console.ReadLine() ?? string.Empty, out var startIso))
        {
            ConsoleHelper.PrintError("Invalid start date.");
            return;
        }

        Console.Write("To Date (DD-MM-YYYY): ");
        if (!DateInputHelper.TryParseToIso(Console.ReadLine() ?? string.Empty, out var endIso))
        {
            ConsoleHelper.PrintError("Invalid end date.");
            return;
        }

        var startDate = DateOnly.Parse(startIso);
        var endDate = DateOnly.Parse(endIso);
        if (endDate <= startDate)
        {
            ConsoleHelper.PrintError("End date must be after start date.");
            return;
        }

        ConsoleHelper.PrintDivider();
        Console.Write("[C] Confirm Allocation     [B] Back: ");
        var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (confirm != "C")
            return;

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
}
