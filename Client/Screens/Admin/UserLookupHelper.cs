using Client.HttpClients;

namespace Client.Screens.Admin;

internal static class UserLookupHelper
{
    public static async Task<UserListItem?> ResolveUserAsync(AppClients clients, string input)
    {
        var list = await clients.Admin.GetUsersAsync();
        if (list is null)
            return null;

        if (long.TryParse(input, out var id))
            return list.Users.FirstOrDefault(u => u.Id == id);

        return list.Users.FirstOrDefault(u =>
            u.Username.Equals(input, StringComparison.OrdinalIgnoreCase));
    }
}
