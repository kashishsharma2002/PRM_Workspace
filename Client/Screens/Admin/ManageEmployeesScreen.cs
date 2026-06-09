using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ManageEmployeesScreen
{
    public static async Task RunAsync(RestClient client)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Manage Employees");
            Console.WriteLine("1. View All Employees");
            Console.WriteLine("2. Update Employee");
            Console.WriteLine("3. Deactivate Employee");
            Console.WriteLine("4. Manage Employee Skills");
            Console.WriteLine("5. Assign Manager");
            Console.WriteLine("6. Back");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ViewAllEmployeesScreen.RunAsync(client);
                        break;
                    case "2":
                        await UpdateEmployeeScreen.RunAsync(client);
                        break;
                    case "3":
                        await DeactivateEmployeeScreen.RunAsync(client);
                        break;
                    case "4":
                        await ManageEmployeeSkillsScreen.RunAsync(client);
                        break;
                    case "5":
                        await AssignManagerScreen.RunAsync(client);
                        break;
                    case "6":
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
