using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ViewRoleCapabilitiesScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            var rolesResponse = await clients.Admin.GetRolesAsync();
            if (!ApiLoadHelper.RequireLoaded(rolesResponse, "Failed to load roles."))
                return;

            ConsoleHelper.PrintHeader("View Role Capabilities");
            Console.WriteLine("Access is controlled by system role (ADMIN / MANAGER / EMPLOYEE).");
            Console.WriteLine("Change a user's role to change their access across the system.");
            ConsoleHelper.PrintDivider();

            foreach (var role in rolesResponse.Roles)
            {
                Console.WriteLine(
                    $"{RoleDisplayHelper.Format(role.RoleName),-20} {role.UserCount,3} users   {role.PermissionCount,3} capabilities");
            }

            ConsoleHelper.PrintDivider();
            Console.WriteLine("Select role: (1) Admin  (2) Manager  (3) Employee/Resource  [B] Back");
            Console.Write("Choice: ");
            var choice = Console.ReadLine()?.Trim();
            if (string.Equals(choice, MenuChoices.Back, StringComparison.OrdinalIgnoreCase))
                return;

            var roleName = RoleDisplayHelper.ResolveSystemRoleChoice(choice);

            if (string.IsNullOrEmpty(roleName))
                return;

            await DisplayRoleCapabilitiesAsync(clients, roleName);
        });

    private static async Task DisplayRoleCapabilitiesAsync(AppClients clients, string roleName)
    {
        var response = await clients.Admin.GetRolePermissionsAsync(roleName);
        if (!ApiLoadHelper.RequireLoaded(response, "Failed to load role capabilities."))
            return;

        ConsoleHelper.PrintHeader($"Capabilities — {RoleDisplayHelper.Format(roleName)}");
        Console.WriteLine($"{response.Permissions.Count} capability(ies) included with this role:");
        ConsoleHelper.PrintDivider();

        foreach (var group in response.Permissions.GroupBy(p => p.Resource).OrderBy(g => g.Key))
        {
            Console.WriteLine($"[{group.Key}]");
            foreach (var permission in group.OrderBy(p => p.Action))
                Console.WriteLine($"  • {permission.Action} — {permission.Description}");

            Console.WriteLine();
        }

        ConsoleHelper.PrintDivider();
        Console.WriteLine("Press any key to return...");
        Console.ReadKey(intercept: true);
    }
}
