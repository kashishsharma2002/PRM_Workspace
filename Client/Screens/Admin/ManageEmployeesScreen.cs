using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ManageEmployeesScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Manage Employees/Resources");
            Console.WriteLine("1. View All Employees/Resources");
            Console.WriteLine("2. Update Employee/Resource");
            Console.WriteLine("3. Deactivate Employee/Resource");
            Console.WriteLine("4. Manage Employee/Resource Skills");
            Console.WriteLine("5. Assign Manager");
            Console.WriteLine("6. Back");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            if (choice is "6" or MenuChoices.Exit)
                return;

            try
            {
                switch (choice)
                {
                    case MenuChoices.One:
                        await ViewAllEmployeesScreen.RunAsync(clients);
                        break;
                    case MenuChoices.Two:
                        await UpdateEmployeeScreen.RunAsync(clients);
                        break;
                    case MenuChoices.Three:
                        await DeactivateEmployeeScreen.RunAsync(clients);
                        break;
                    case "4":
                        await ManageEmployeeSkillsScreen.RunAsync(clients);
                        break;
                    case "5":
                        await AssignManagerScreen.RunAsync(clients);
                        break;
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
