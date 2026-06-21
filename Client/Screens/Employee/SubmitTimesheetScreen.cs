using Client.Common;
using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Employee;

public static class SubmitTimesheetScreen
{
    public static Task RunAsync(AppClients clients, DateOnly? defaultWeekStart = null) =>
        ScreenRunner.RunSafeAsync(() => RunCoreAsync(clients, defaultWeekStart));

    private static async Task RunCoreAsync(AppClients clients, DateOnly? defaultWeekStart)
    {
        var reminder = await clients.Employee.GetReminderAsync();
        if (reminder?.IsTimesheetFrozen == true)
        {
            ConsoleHelper.PrintError(
                "Timesheet submission is not available. Your access is frozen — contact your manager to restore it.");
            return;
        }

        ConsoleHelper.PrintHeader("Submit Timesheet");
        if (defaultWeekStart is not null)
        {
            Console.WriteLine(
                $"Week Start: Enter date (DD-MM-YYYY) or press Enter for {DateInputHelper.FormatDisplay(defaultWeekStart.Value)} (missed week)");
        }
        else
        {
            Console.WriteLine("Week Start: Enter any date in the week (DD-MM-YYYY) or press Enter for last Monday");
        }

        Console.Write("> ");
        var weekInput = Console.ReadLine();

        if (!DateInputHelper.TryParseWeekStart(weekInput, out var weekStart, out var dateError, defaultWeekStart))
        {
            ConsoleHelper.PrintError(dateError ?? "Invalid week start date.");
            return;
        }

        var allocations = await clients.Employee.GetWeekAllocationsAsync(weekStart);

        if (allocations is null || allocations.Count == 0)
        {
            ConsoleHelper.PrintError("You have no active allocations for this week.");
            return;
        }

        var tags = await clients.Employee.GetActivityTagsAsync();
        if (tags is null || tags.Count == 0)
        {
            ConsoleHelper.PrintError("Activity tags could not be loaded.");
            return;
        }

        var lineItems = new List<TimesheetLineItemRequest>();
        var projectIndex = 1;

        foreach (var allocation in allocations)
        {
            ConsoleHelper.PrintDivider();
            Console.WriteLine($"PROJECT {projectIndex} OF {allocations.Count} — {allocation.ProjectName}");
            Console.WriteLine($"  Allocation: {allocation.AllocationPercentage:0.#}%   |   Expected: {allocation.MaxHours:0.#} hrs max");
            ConsoleHelper.PrintDivider();

            decimal hours;
            while (true)
            {
                Console.Write("Hours worked this week: ");
                if (decimal.TryParse(Console.ReadLine()?.Trim(), out hours) && hours > 0)
                    break;

                ConsoleHelper.PrintError("Hours must be a positive number.");
            }

            Console.WriteLine("What did you work on? Select activity tags:");
            for (var i = 0; i < tags.Count; i++)
                Console.WriteLine($"  {i + 1,2}.  {tags[i].TagName}");

            List<long> selectedTagIds;
            string? customTagText = null;
            while (true)
            {
                Console.Write("Select tags (comma-separated numbers): ");
                var tagInput = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(tagInput))
                {
                    ConsoleHelper.PrintError("At least one activity tag is required.");
                    continue;
                }

                selectedTagIds = [];
                customTagText = null;
                var tagSelectionValid = true;
                foreach (var part in tagInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!int.TryParse(part, out var tagNumber) || tagNumber < 1 || tagNumber > tags.Count)
                    {
                        ConsoleHelper.PrintError("Invalid tag selection.");
                        tagSelectionValid = false;
                        break;
                    }

                    var tag = tags[tagNumber - 1];
                    selectedTagIds.Add(tag.Id);

                    if (tag.TagCode == "OTHER")
                    {
                        Console.Write("Enter custom activity description: ");
                        customTagText = Console.ReadLine()?.Trim();
                        if (string.IsNullOrWhiteSpace(customTagText))
                        {
                            ConsoleHelper.PrintError("Custom tag text is required when selecting Other.");
                            tagSelectionValid = false;
                            break;
                        }
                    }
                }

                if (tagSelectionValid && selectedTagIds.Count > 0)
                    break;
            }

            lineItems.Add(new TimesheetLineItemRequest
            {
                ProjectId = allocation.ProjectId,
                HoursLogged = hours,
                ActivityTagIds = selectedTagIds.Distinct().ToList(),
                CustomTagText = customTagText
            });

            projectIndex++;
        }

        var totalHours = lineItems.Sum(li => li.HoursLogged);
        ConsoleHelper.PrintDivider();
        Console.WriteLine("SUMMARY");
        foreach (var item in lineItems)
        {
            var projectName = allocations.First(a => a.ProjectId == item.ProjectId).ProjectName;
            Console.WriteLine($"  {projectName,-16} {item.HoursLogged,5:0.#} hrs");
        }

        Console.WriteLine($"  {"Total",-16} {totalHours,5:0.#} hrs");
        ConsoleHelper.PrintDivider();
        Console.Write("[S] Submit Timesheet  [B] Back — choice: ");
        var choice = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (choice != MenuChoices.Save)
            return;

        var request = new TimesheetSubmitRequest
        {
            WeekStartDate = weekStart,
            LineItems = lineItems
        };

        var response = await clients.Employee.SubmitTimesheetAsync(request);
        if (!ApiLoadHelper.RequireLoaded(response, "Failed to submit timesheet."))
            return;

        ConsoleHelper.PrintSuccess($"Timesheet submitted successfully. Status: {response.Status}");
    }
}
