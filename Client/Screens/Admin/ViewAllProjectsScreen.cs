using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ViewAllProjectsScreen
{
    public static async Task RunAsync(RestClient client)
    {
        try
        {
            var list = await client.GetAsync<ProjectListResponse>("/api/projects", requireAuth: true);
            if (list is null)
            {
                ConsoleHelper.PrintError("Failed to load projects.");
                return;
            }

            ConsoleHelper.PrintHeader("All Projects");
            Console.WriteLine($"{"ID",-6}{"Name",-18}{"Manager",-14}{"End Date",-12}{"Status",-10}{"SP Done/Total"}");
            ConsoleHelper.PrintDivider();

            foreach (var project in list.Projects)
            {
                Console.WriteLine($"{project.Id,-6}{project.ProjectName,-18}{project.ManagerName,-14}" +
                    $"{DateInputHelper.FormatDisplay(project.EndDate),-12}{project.ProjectStatus,-10}" +
                    $"{project.StoryPointsDone} / {project.TotalStoryPoints}");
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine("Press any key to return...");
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }
        catch (SessionExpiredException) { throw; }
        catch (Exception ex) { ErrorDisplayHelper.HandleException(ex); }
    }
}
