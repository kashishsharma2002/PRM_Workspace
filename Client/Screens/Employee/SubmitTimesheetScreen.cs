using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Employee;

public static class SubmitTimesheetScreen
{
    public static async Task RunAsync(RestClient client)
    {
        try
        {
            ConsoleHelper.PrintHeader("Submit Timesheet");
            Console.WriteLine("Week Start: Enter date (DD-MM-YYYY) or press Enter for last Monday");
            Console.Write("> ");
            var weekInput = Console.ReadLine();

            if (!DateInputHelper.TryParseWeekStart(weekInput, out var weekStart, out var dateError))
            {
                ConsoleHelper.PrintError(dateError ?? "Invalid week start date.");
                return;
            }

            var allocations = await client.GetAsync<List<EmployeeWeekAllocation>>(
                $"/api/timesheets/week-allocations?weekStart={weekStart:yyyy-MM-dd}",
                requireAuth: true);

            if (allocations is null || allocations.Count == 0)
            {
                ConsoleHelper.PrintError("You have no active allocations for this week.");
                return;
            }

            var tags = await client.GetAsync<List<ActivityTagItem>>("/api/activity-tags", requireAuth: true);
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

                Console.Write("Hours worked this week: ");
                if (!decimal.TryParse(Console.ReadLine()?.Trim(), out var hours) || hours <= 0)
                {
                    ConsoleHelper.PrintError("Hours must be a positive number.");
                    return;
                }

                Console.WriteLine("What did you work on? Select activity tags:");
                for (var i = 0; i < tags.Count; i++)
                    Console.WriteLine($"  {i + 1,2}.  {tags[i].TagName}");

                Console.Write("Select tags (comma-separated numbers): ");
                var tagInput = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(tagInput))
                {
                    ConsoleHelper.PrintError("At least one activity tag is required.");
                    return;
                }

                var selectedTagIds = new List<long>();
                string? customTagText = null;
                foreach (var part in tagInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!int.TryParse(part, out var tagNumber) || tagNumber < 1 || tagNumber > tags.Count)
                    {
                        ConsoleHelper.PrintError("Invalid tag selection.");
                        return;
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
                            return;
                        }
                    }
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
            if (choice != "S")
                return;

            var request = new TimesheetSubmitRequest
            {
                WeekStartDate = weekStart,
                LineItems = lineItems
            };

            var response = await client.PostAsync<TimesheetSubmitResponse>("/api/timesheets", request, requireAuth: true);
            if (response is null)
            {
                ConsoleHelper.PrintError("Failed to submit timesheet.");
                return;
            }

            ConsoleHelper.PrintSuccess($"Timesheet submitted successfully. Status: {response.Status}");
        }
        catch (SessionExpiredException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError(ex.Message);
        }
    }
}
