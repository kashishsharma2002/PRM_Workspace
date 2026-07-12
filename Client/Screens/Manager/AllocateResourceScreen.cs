using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Manager;

public static partial class AllocateResourceScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Allocate Resource");
            Console.WriteLine("1. Find employee/resource using AI");
            Console.WriteLine("2. Allocate directly (I already know who I want)");
            Console.WriteLine("3. End an existing allocation");
            Console.WriteLine("4. Back");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            await ScreenRunner.RunSafeAsync(async () =>
            {
                switch (choice)
                {
                    case MenuChoices.One:
                        await RunAiAllocationFlowAsync(clients);
                        break;
                    case MenuChoices.Two:
                        await RunDirectAllocationAsync(clients);
                        break;
                    case MenuChoices.Three:
                        await RunEndAllocationAsync(clients);
                        break;
                    case MenuChoices.Four:
                        return;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            });

            if (choice == MenuChoices.Four)
                return;
        }
    }
}
