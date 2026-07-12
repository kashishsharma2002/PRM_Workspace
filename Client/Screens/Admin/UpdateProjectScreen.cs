using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class UpdateProjectScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            ConsoleHelper.PrintHeader("Update Project Details");

            Console.Write("Enter Project ID: ");
            if (!long.TryParse(Console.ReadLine()?.Trim(), out var projectId))
            {
                ConsoleHelper.PrintError("Invalid project ID.");
                return;
            }

            var detail = await clients.Admin.GetProjectAsync(projectId);
            if (!ApiLoadHelper.RequireLoaded(detail, "Project not found."))
                return;

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
            if (!string.IsNullOrWhiteSpace(startInput))
            {
                if (!DateInputHelper.TryParseToIso(startInput, out var startIso))
                {
                    ConsoleHelper.PrintError("Invalid start date. Use format DD-MM-YYYY.");
                    return;
                }

                startDate = DateOnly.Parse(startIso);
                if (startDate < DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    ConsoleHelper.PrintError("Start date cannot be in the past.");
                    return;
                }
            }

            Console.Write($"End Date             : [{DateInputHelper.FormatDisplay(detail.EndDate)}] (DD-MM-YYYY) ");
            var endInput = Console.ReadLine()?.Trim();
            var endDate = detail.EndDate;
            if (!string.IsNullOrWhiteSpace(endInput))
            {
                if (!DateInputHelper.TryParseToIso(endInput, out var endIso))
                {
                    ConsoleHelper.PrintError("Invalid end date. Use format DD-MM-YYYY.");
                    return;
                }

                endDate = DateOnly.Parse(endIso);
            }

            if (endDate <= startDate)
            {
                ConsoleHelper.PrintError("End date must be after start date.");
                return;
            }

            Console.WriteLine("Status               : (1) PLANNED  (2) ACTIVE  (3) ON_HOLD  (4) COMPLETED");
            Console.Write($"Current: {detail.ProjectStatus} — new [1-4] or Enter to keep: ");
            var statusChoice = Console.ReadLine()?.Trim();
            var status = statusChoice switch
            {
                MenuChoices.One => ProjectStatusConstants.Planned,
                MenuChoices.Two => ProjectStatusConstants.Active,
                MenuChoices.Three => ProjectStatusConstants.OnHold,
                MenuChoices.Four => ProjectStatusConstants.Completed,
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
            if (!FormInputHelper.ConfirmSave())
                return;

            await clients.Admin.UpdateProjectAsync(projectId, new UpdateProjectRequest
            {
                ProjectName = name,
                Description = string.IsNullOrWhiteSpace(description) ? detail.Description : description,
                StartDate = startDate,
                EndDate = endDate,
                ProjectStatus = status,
                ManagerUserId = managerId,
                TotalStoryPoints = storyPoints
            });

            ConsoleHelper.PrintSuccess("Project updated.");
        });
}
