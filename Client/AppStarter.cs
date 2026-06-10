using Client.Helpers;
using Client.HttpClients;
using Client.Screens;
using Client.Screens.Admin;
using Client.Screens.Employee;
using Client.Screens.Manager;

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

            var loggedOut = await RouteToMenuAsync(client);
            if (loggedOut)
                continue;
        }
    }

    private static async Task<bool> RouteToMenuAsync(RestClient client)
    {
        switch (SessionStore.Role)
        {
            case "ADMIN":
                return !await AdminMenuScreen.RunAsync(client);
            case "MANAGER":
                return !await ManagerMenuScreen.RunAsync(client);
            case "EMPLOYEE":
                return !await EmployeeMenuScreen.RunAsync(client);
            default:
                ConsoleHelper.PrintError($"Unknown role: {SessionStore.Role}");
                return true;
        }
    }
}
