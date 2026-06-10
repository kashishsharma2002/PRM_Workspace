using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class UpdateProjectScreen
{
    public static async Task RunAsync(RestClient client)
    {
        ConsoleHelper.PrintHeader("Update Project Details");

        Console.Write("Enter Project ID: ");
        if (!long.TryParse(Console.ReadLine()?.Trim(), out var projectId))
        {
            ConsoleHelper.PrintError("Invalid project ID.");
            return;
        }

        try
        {
            var detail = await client.GetAsync<ProjectDetail>($"/api/projects/{projectId}", requireAuth: true);
            if (detail is null)
            {
                ConsoleHelper.PrintError("Project not found.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"── {detail.ProjectName} ───────────────────────────────");
            Console.Write($"Project Name         : [{detail.ProjectName}] ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(name)) name = detail.ProjectName;

            Console.Write($"Description          : [{detail.Description ?? ""}] ");
            var description = Console.ReadLine()?.Trim();

            Console.Write($"Start Date           : [{DateInputHelper.FormatDisplay(detail.StartDate)}] (DD-MM-YYYY) ");
            var startInput = Console.ReadLine()?.Trim();
            var startDate = detail.StartDate;
            if (!string.IsNullOrWhiteSpace(startInput) && DateInputHelper.TryParseToIso(startInput, out var startIso))
                startDate = DateOnly.Parse(startIso);

            Console.Write($"End Date             : [{DateInputHelper.FormatDisplay(detail.EndDate)}] (DD-MM-YYYY) ");
            var endInput = Console.ReadLine()?.Trim();
            var endDate = detail.EndDate;
            if (!string.IsNullOrWhiteSpace(endInput) && DateInputHelper.TryParseToIso(endInput, out var endIso))
                endDate = DateOnly.Parse(endIso);

            Console.WriteLine("Status               : (1) PLANNED  (2) ACTIVE  (3) ON_HOLD  (4) COMPLETED");
            Console.Write($"Current: {detail.ProjectStatus} — new [1-4] or Enter to keep: ");
            var statusChoice = Console.ReadLine()?.Trim();
            var status = statusChoice switch
            {
                "1" => "PLANNED",
                "2" => "ACTIVE",
                "3" => "ON_HOLD",
                "4" => "COMPLETED",
                "" => detail.ProjectStatus,
                _ => detail.ProjectStatus
            };

            Console.Write($"Assign Manager       : [{detail.ManagerUserId}] ");
            var mgrInput = Console.ReadLine()?.Trim();
            var managerId = detail.ManagerUserId;
            if (!string.IsNullOrWhiteSpace(mgrInput) && long.TryParse(mgrInput, out var parsedMgr))
                managerId = parsedMgr;

            Console.Write($"Total Story Points   : [{detail.TotalStoryPoints}] ");
            var spInput = Console.ReadLine()?.Trim();
            var storyPoints = detail.TotalStoryPoints;
            if (!string.IsNullOrWhiteSpace(spInput) && int.TryParse(spInput, out var parsedSp))
                storyPoints = parsedSp;

            ConsoleHelper.PrintDivider();
            Console.Write("[S] Save  [B] Back — choice: ");
            if (Console.ReadLine()?.Trim().ToUpperInvariant() != "S")
                return;

            await client.PutAsync<object>($"/api/projects/{projectId}", new UpdateProjectRequest
            {
                ProjectName = name,
                Description = string.IsNullOrWhiteSpace(description) ? detail.Description : description,
                StartDate = startDate,
                EndDate = endDate,
                ProjectStatus = status,
                ManagerUserId = managerId,
                TotalStoryPoints = storyPoints
            }, requireAuth: true);

            ConsoleHelper.PrintSuccess("Project updated.");
        }
        catch (SessionExpiredException) { throw; }
        catch (Exception ex) { ErrorDisplayHelper.HandleException(ex); }
    }
}
