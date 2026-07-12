using Client.Common;

using Client.Helpers;

using Client.HttpClients;



namespace Client.Screens.Admin;



public static class ManageUsersScreen

{

    public static async Task RunAsync(AppClients clients)

    {

        while (true)

        {

            ConsoleHelper.PrintHeader("Manage Users");

            Console.WriteLine("1. Create User Account");

            Console.WriteLine("2. View All Users");

            Console.WriteLine("3. Reset User Password");

            Console.WriteLine("4. Deactivate User");

            Console.WriteLine("5. Change User Role");

            Console.WriteLine("6. View Role Capabilities");

            Console.WriteLine("7. Back");

            ConsoleHelper.PrintDivider();

            Console.Write("Enter option: ");

            var choice = Console.ReadLine()?.Trim();



            if (choice is "7" or MenuChoices.Exit)

                return;



            if (!await ScreenRunner.TryRunMenuActionAsync(async () =>

            {

                switch (choice)

                {

                    case MenuChoices.One:

                        await CreateUserAccountScreen.RunAsync(clients);

                        break;

                    case MenuChoices.Two:

                        await ViewAllUsersScreen.RunAsync(clients);

                        break;

                    case MenuChoices.Three:

                        await ResetPasswordScreen.RunAsync(clients);

                        break;

                    case "4":

                        await DeactivateUserScreen.RunAsync(clients);

                        break;

                    case "5":

                        await ChangeUserRoleScreen.RunAsync(clients);

                        break;

                    case "6":

                        await ViewRoleCapabilitiesScreen.RunAsync(clients);

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


