using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Manager;

public static partial class AllocateResourceScreen
{
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
        if (confirm != MenuChoices.Yes)
            return;

        var result = await clients.Manager.EndAllocationAsync(selected.AllocationId);

        if (result is not null)
        {
            ConsoleHelper.PrintSuccess(
                $"Allocation ended. {selected.EmployeeName} (Employee/Resource) freed from {detail.ProjectName} as of " +
                $"{DateInputHelper.FormatDisplay(result.AllocationEndDate)}.");
        }
    }
}
