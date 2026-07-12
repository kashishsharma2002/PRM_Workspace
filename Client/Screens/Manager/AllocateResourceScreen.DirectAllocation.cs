using Client.Common;
using Client.Helpers;
using Client.HttpClients;
using Client.Models.Allocations;

namespace Client.Screens.Manager;

public static partial class AllocateResourceScreen
{
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
                .Where(p => p.ProjectStatus.Equals(ProjectStatusConstants.Planned, StringComparison.OrdinalIgnoreCase) ||
                            p.ProjectStatus.Equals(ProjectStatusConstants.Active, StringComparison.OrdinalIgnoreCase))
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
            if (string.Equals(projectInput, MenuChoices.Back, StringComparison.OrdinalIgnoreCase))
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

            var startDate = FormInputHelper.PromptDateInRange(
                selectedProject.StartDate,
                selectedProject.EndDate,
                "From Date",
                allowPastDates: false);
            var endDate = FormInputHelper.PromptDateAfter(startDate, "To Date");
            while (endDate > selectedProject.EndDate)
            {
                ConsoleHelper.PrintError(
                    $"To Date cannot be after the project end date ({DateInputHelper.FormatDisplay(selectedProject.EndDate)}).");
                endDate = FormInputHelper.PromptDateAfter(startDate, "To Date");
            }

            ConsoleHelper.PrintDivider();
            Console.Write("[C] Confirm Allocation     [B] Back: ");
            var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (confirm == MenuChoices.Back)
                return;
            if (confirm != MenuChoices.Confirm)
            {
                ConsoleHelper.PrintError("Invalid choice. Enter C to confirm or B to go back.");
                continue;
            }

            var saved = false;
            await ScreenRunner.RunSafeAsync(async () =>
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
                    saved = true;
                    return;
                }

                ConsoleHelper.PrintError("Allocation could not be saved. Please try again.");
            });

            if (saved)
                return;
        }
    }
}
