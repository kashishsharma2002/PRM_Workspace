using Client.Common;
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
        var clients = new AppClients(serverBaseUrl);

        while (true)
        {
            switch (WelcomeScreen.Run())
            {
                case WelcomeChoice.Exit:
                    return;
            }

            SessionStore.Clear();
            clients.SetToken(null);

            if (!await LoginScreen.RunAsync(clients))
            {
                Console.WriteLine();
                continue;
            }

            if (SessionStore.ForcePasswordChange)
            {
                while (!await ChangePasswordScreen.RunAsync(clients))
                {
                    Console.WriteLine("Press any key to retry password change...");
                    Console.ReadKey(intercept: true);
                    Console.WriteLine();
                }
            }

            var loggedOut = await RouteToMenuAsync(clients);
            if (loggedOut)
                continue;
        }
    }

    private static async Task<bool> RouteToMenuAsync(AppClients clients)
    {
        switch (SessionStore.Role)
        {
            case RoleConstants.Admin:
                return !await AdminMenuScreen.RunAsync(clients);
            case RoleConstants.Manager:
                return !await ManagerMenuScreen.RunAsync(clients);
            case RoleConstants.Employee:
                return !await EmployeeMenuScreen.RunAsync(clients);
            default:
                ConsoleHelper.PrintError($"Unknown role: {SessionStore.Role}");
                return true;
        }
    }
}
