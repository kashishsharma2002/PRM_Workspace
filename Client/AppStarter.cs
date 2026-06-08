using Client.Helpers;
using Client.HttpClients;
using Client.Screens;
using Client.Screens.Admin;
using Client.Screens.Employee;
using Client.Screens.Manager;
using Client.Session;

namespace Client;

public static class AppStarter
{
    public static async Task RunAsync(string serverBaseUrl)
    {
        var client = new RestClient(serverBaseUrl);

        while (true)
        {
            SessionStore.Clear();
            client.SetToken(null);

            if (!await LoginScreen.RunAsync(client))
            {
                Console.WriteLine("Press any key to retry or Ctrl+C to exit...");
                Console.ReadKey(intercept: true);
                Console.WriteLine();
                continue;
            }

            if (SessionStore.ForcePasswordChange)
            {
                while (!await ChangePasswordScreen.RunAsync(client))
                {
                    Console.WriteLine("Press any key to retry password change...");
                    Console.ReadKey(intercept: true);
                    Console.WriteLine();
                }
            }

            RouteToMenu();
            break;
        }
    }

    private static void RouteToMenu()
    {
        switch (SessionStore.Role)
        {
            case "ADMIN":
                AdminMenuScreen.Run();
                break;
            case "MANAGER":
                ManagerMenuScreen.Run();
                break;
            case "EMPLOYEE":
                EmployeeMenuScreen.Run();
                break;
            default:
                ConsoleHelper.PrintError($"Unknown role: {SessionStore.Role}");
                break;
        }
    }
}
