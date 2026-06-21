using Client.Common;
using Client.Common.Audit;
using Client.Helpers;
using Client.HttpClients;
using System.Globalization;

namespace Client.Screens.Admin;

public static class ActivityLogScreen
{
    private const int DefaultPageSize = 10;

    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            var page = 1;
            DateTime? from = DateTime.UtcNow.Date.AddDays(-7);
            DateTime? to = null;
            long? actorUserId = null;
            string? entityName = null;
            string? search = null;

            while (true)
            {
                var query = BuildQuery(from, to, actorUserId, entityName, search, page);
                var response = await clients.Admin.GetAuditLogsAsync(query);
                if (!ApiLoadHelper.RequireLoaded(response, "Failed to load activity log."))
                    return;

                ConsoleHelper.PrintHeader(BuildTitle(from));
                if (response.Items.Count == 0)
                {
                    Console.WriteLine("No activity found for the selected filters.");
                }
                else
                {
                    foreach (var item in response.Items)
                    {
                        var localTime = item.OccurredAt.ToLocalTime();
                        var actorRole = string.IsNullOrWhiteSpace(item.ActorRole)
                            ? ""
                            : $" ({RoleDisplayHelper.Format(item.ActorRole)})";
                        Console.WriteLine($"{localTime:dd-MMM-yyyy HH:mm}  |  {item.ActorName}{actorRole}");
                        Console.WriteLine($"  {item.Summary}");
                        Console.WriteLine();
                    }
                }

                ConsoleHelper.PrintDivider();
                Console.WriteLine(
                    $"Page {response.Page}/{Math.Max(response.TotalPages, 1)}   |   Total entries: {response.TotalCount}");
                ConsoleHelper.PrintDivider();
                Console.WriteLine("[F] Filter  [N] Next page  [P] Previous page  [B] Back");
                Console.Write("Choice: ");
                var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

                switch (choice)
                {
                    case "F":
                        ApplyFilters(ref from, ref to, ref actorUserId, ref entityName, ref search);
                        page = 1;
                        break;
                    case "N" when page < response.TotalPages:
                        page++;
                        break;
                    case "P" when page > 1:
                        page--;
                        break;
                    case MenuChoices.Back:
                    case MenuChoices.Exit:
                        return;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            }
        });

    private static string BuildTitle(DateTime? from)
    {
        if (from.HasValue)
            return $"Activity Log — since {from.Value.ToLocalTime():dd-MMM-yyyy}";

        return "Activity Log";
    }

    private static void ApplyFilters(
        ref DateTime? from,
        ref DateTime? to,
        ref long? actorUserId,
        ref string? entityName,
        ref string? search)
    {
        ConsoleHelper.PrintHeader("Filter Activity Log");

        Console.Write($"From date (yyyy-MM-dd) [{from?.ToLocalTime():yyyy-MM-dd}]: ");
        var fromInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(fromInput) &&
            DateTime.TryParse(fromInput, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedFrom))
            from = parsedFrom.ToUniversalTime();

        Console.Write("To date (yyyy-MM-dd, optional): ");
        var toInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(toInput) &&
            DateTime.TryParse(toInput, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedTo))
            to = parsedTo.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
        else
            to = null;

        Console.Write("Actor User ID (optional): ");
        var actorInput = Console.ReadLine()?.Trim();
        actorUserId = long.TryParse(actorInput, out var actorId) ? actorId : null;

        Console.WriteLine("Entity type: (1) All  (2) Users  (3) Employees  (4) Projects  (5) Timesheets  (6) Sign-in");
        Console.Write("Choice [1-6]: ");
        entityName = Console.ReadLine()?.Trim() switch
        {
            MenuChoices.Two => AuditEntityConstants.Users,
            MenuChoices.Three => AuditEntityConstants.Employees,
            "4" => AuditEntityConstants.Projects,
            "5" => AuditEntityConstants.Timesheets,
            "6" => AuditEntityConstants.Auth,
            _ => null
        };

        Console.Write("Search text (optional): ");
        search = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(search))
            search = null;
    }

    private static string BuildQuery(
        DateTime? from,
        DateTime? to,
        long? actorUserId,
        string? entityName,
        string? search,
        int page)
    {
        var parts = new List<string>
        {
            $"page={page}",
            $"pageSize={DefaultPageSize}"
        };

        if (from.HasValue)
            parts.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");

        if (to.HasValue)
            parts.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");

        if (actorUserId.HasValue)
            parts.Add($"actorUserId={actorUserId.Value}");

        if (!string.IsNullOrWhiteSpace(entityName))
            parts.Add($"entityName={Uri.EscapeDataString(entityName)}");

        if (!string.IsNullOrWhiteSpace(search))
            parts.Add($"search={Uri.EscapeDataString(search)}");

        return string.Join('&', parts);
    }
}
