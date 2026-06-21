using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ViewAllProjectsScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            var list = await clients.Admin.GetProjectsAsync();
            if (!ApiLoadHelper.RequireLoaded(list, "Failed to load projects."))
                return;

            ConsoleHelper.PrintHeader("All Projects");
            Console.WriteLine($"{"ID",-6}{"Name",-16}{"Manager",-14}{"End Date",-12}{"Status",-10}{"Health",-8}{"SP Done/Total"}");
            ConsoleHelper.PrintDivider();

            foreach (var project in list.Projects)
            {
                Console.WriteLine($"{project.Id,-6}{project.ProjectName,-16}{project.ManagerName,-14}" +
                    $"{DateInputHelper.FormatDisplay(project.EndDate),-12}{project.ProjectStatus,-10}{project.HealthStatus,-8}" +
                    $"{project.StoryPointsDone} / {project.TotalStoryPoints}");
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine("Press any key to return...");
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        });
}
