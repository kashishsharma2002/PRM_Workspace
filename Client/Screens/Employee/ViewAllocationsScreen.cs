using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Employee;

public static class ViewAllocationsScreen
{
    public static async Task RunAsync(AppClients clients)
    {
        try
        {
            ConsoleHelper.PrintHeader("My Allocations");
            var response = await clients.Employee.GetMyAllocationsAsync();
            if (response is null || response.Allocations.Count == 0)
            {
                Console.WriteLine("No allocations found.");
                ConsoleHelper.PrintDivider();
                Console.WriteLine("Press any key to go back...");
                Console.ReadKey(intercept: true);
                return;
            }

            Console.WriteLine($"{"Project",-18}{"%",-6}{"From",-12}{"To",-12}{"Status"}");
            ConsoleHelper.PrintDivider();
            foreach (var item in response.Allocations)
            {
                Console.WriteLine(
                    $"{item.ProjectName,-18}{item.AllocationPercentage,4:0.#}%  " +
                    $"{DateInputHelper.FormatDisplay(item.AllocationStartDate),-12}" +
                    $"{DateInputHelper.FormatDisplay(item.AllocationEndDate),-12}" +
                    $"{item.AllocationStatus}");
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine($"Total Utilisation: {response.TotalUtilizationPercentage:0.#}%");
            ConsoleHelper.PrintDivider();
            Console.WriteLine("Press any key to go back...");
            Console.ReadKey(intercept: true);
        }
        catch (SessionExpiredException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorDisplayHelper.HandleException(ex);
        }
    }
}
