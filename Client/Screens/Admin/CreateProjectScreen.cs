using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class CreateProjectScreen
{
    public static Task RunAsync(AppClients clients) =>
        ScreenRunner.RunSafeAsync(async () =>
        {
            ConsoleHelper.PrintHeader("Create Project");

            var name = FormInputHelper.PromptRequired("Project Name");
            Console.Write("Description         : ");
            var description = Console.ReadLine()?.Trim();

            var startDate = FormInputHelper.PromptDateNotBeforeToday("Start Date");
            var endDate = FormInputHelper.PromptDate("End Date");

            while (endDate <= startDate)
            {
                ConsoleHelper.PrintError($"End date must be after start date.");
                endDate = FormInputHelper.PromptDate("End Date");
            }

            var status = ProjectStatusInputHelper.PromptCreateStatus();

            var managerId = FormInputHelper.PromptId("Assign Manager (User ID)");
            var storyPoints = FormInputHelper.PromptInt("Total Story Points", minValue: 0);

            ConsoleHelper.PrintDivider();
            if (!FormInputHelper.ConfirmSave())
                return;

            var result = await clients.Admin.CreateProjectAsync(new CreateProjectRequest
            {
                ProjectName = name,
                Description = description,
                StartDate = startDate,
                EndDate = endDate,
                ProjectStatus = status,
                ManagerUserId = managerId,
                TotalStoryPoints = storyPoints
            });

            if (result is not null)
                ConsoleHelper.PrintSuccess($"Project created ({result.ProjectCode}).");
        });
}
