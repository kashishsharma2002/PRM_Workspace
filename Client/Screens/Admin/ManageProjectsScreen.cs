using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ManageProjectsScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Manage Projects");
            Console.WriteLine("1. Create Project");
            Console.WriteLine("2. View All Projects");
            Console.WriteLine("3. Update Project Details");
            Console.WriteLine("4. Manage Milestones");
            Console.WriteLine("5. Back");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            if (choice is "5" or MenuChoices.Exit)
                return;

            if (!await ScreenRunner.TryRunMenuActionAsync(async () =>
            {
                switch (choice)
                {
                    case MenuChoices.One:
                        await CreateProjectScreen.RunAsync(clients);
                        break;
                    case MenuChoices.Two:
                        await ViewAllProjectsScreen.RunAsync(clients);
                        break;
                    case MenuChoices.Three:
                        await UpdateProjectScreen.RunAsync(clients);
                        break;
                    case "4":
                        await ManageMilestonesScreen.RunAsync(clients);
                        break;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            }))
                throw new SessionExpiredException("Session expired. Please log in again.");
        }
    }
}
