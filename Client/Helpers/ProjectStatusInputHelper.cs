using Client.Common;

namespace Client.Helpers;

public static class ProjectStatusInputHelper
{
    public static string PromptCreateStatus()
    {
        Console.WriteLine("Status              : (1) PLANNED  (2) ACTIVE  (3) ON_HOLD");
        string status;
        do
        {
            Console.Write("Select status [1-3]: ");
            status = Console.ReadLine()?.Trim() switch
            {
                MenuChoices.One => ProjectStatusConstants.Planned,
                MenuChoices.Two => ProjectStatusConstants.Active,
                MenuChoices.Three => ProjectStatusConstants.OnHold,
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(status))
                ConsoleHelper.PrintError("Invalid status. Enter 1, 2, or 3.");
        } while (string.IsNullOrWhiteSpace(status));

        return status;
    }
}
