using Client.Common;
using Client.Helpers;
using Client.HttpClients;
using Client.Models.Employees;

namespace Client.Screens.Manager;

internal static class ManagerTeamEmployeePicker
{
    private const long ShowTeamListOption = 0;

    public static async Task<long?> PromptEmployeeIdAsync(AppClients clients)
    {
        while (true)
        {
            Console.Write($"Enter Employee/Resource ID (enter {ShowTeamListOption} to view all employees/resources under you, B to go back): ");
            var input = Console.ReadLine()?.Trim();

            if (string.Equals(input, MenuChoices.Back, StringComparison.OrdinalIgnoreCase))
                return null;

            if (!long.TryParse(input, out var enteredValue))
            {
                ConsoleHelper.PrintError("Invalid employee ID.");
                continue;
            }

            if (enteredValue == ShowTeamListOption)
            {
                await DisplayTeamEmployeesAsync(clients);
                continue;
            }

            if (enteredValue < 1)
            {
                ConsoleHelper.PrintError("Invalid employee ID.");
                continue;
            }

            return enteredValue;
        }
    }

    private static async Task DisplayTeamEmployeesAsync(AppClients clients)
    {
        var teamEmployees = await FetchTeamEmployeesAsync(clients);
        if (!ApiLoadHelper.RequireLoaded(teamEmployees, "Could not load your team employees."))
            return;

        if (teamEmployees.Count == 0)
        {
            Console.WriteLine("No employees found under your management.");
            ConsoleHelper.PrintDivider();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"{"Employee/Resource ID",-22}{"Employee/Resource Name"}");
        ConsoleHelper.PrintDivider();

        foreach (var employee in teamEmployees)
            Console.WriteLine($"{employee.Id,-22}{employee.Name}");

        ConsoleHelper.PrintDivider();
    }

    private static async Task<IReadOnlyList<TeamEmployeeSummary>?> FetchTeamEmployeesAsync(AppClients clients)
    {
        var dashboard = await clients.Manager.GetTeamDashboardAsync();
        if (dashboard is null)
            return null;

        return BuildTeamEmployeeList(dashboard);
    }

    private static List<TeamEmployeeSummary> BuildTeamEmployeeList(TeamDashboard dashboard)
    {
        var employeesById = new Dictionary<long, TeamEmployeeSummary>();

        foreach (var employee in dashboard.BenchEmployees)
            employeesById.TryAdd(employee.Id, new TeamEmployeeSummary(employee.Id, employee.Name));

        foreach (var employee in dashboard.ActiveEmployees)
            employeesById.TryAdd(employee.Id, new TeamEmployeeSummary(employee.Id, employee.Name));

        return employeesById.Values.OrderBy(e => e.Name).ToList();
    }
}
