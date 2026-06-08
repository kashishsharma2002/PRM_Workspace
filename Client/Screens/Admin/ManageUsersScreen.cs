using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ManageUsersScreen
{
    public static async Task RunAsync(RestClient client)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Manage Users");
            Console.WriteLine("1. Create User Account");
            Console.WriteLine("2. View All Users");
            Console.WriteLine("3. Reset User Password");
            Console.WriteLine("4. Deactivate User");
            Console.WriteLine("5. Back");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await CreateUserAccountScreen.RunAsync(client);
                    break;
                case "2":
                    await ViewAllUsersScreen.RunAsync(client);
                    break;
                case "3":
                    await ResetPasswordScreen.RunAsync(client);
                    break;
                case "4":
                    await DeactivateUserScreen.RunAsync(client);
                    break;
                case "5":
                case "0":
                    return;
                default:
                    ConsoleHelper.PrintError("Invalid option.");
                    break;
            }
        }
    }
}
