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

            try
            {
                switch (choice)
                {
                    case "1":
                        await CreateProjectScreen.RunAsync(clients);
                        break;
                    case "2":
                        await ViewAllProjectsScreen.RunAsync(clients);
                        break;
                    case "3":
                        await UpdateProjectScreen.RunAsync(clients);
                        break;
                    case "4":
                        await ManageMilestonesScreen.RunAsync(clients);
                        break;
                    case "5":
                    case "0":
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
        }
    }
}
