using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class ManageEmployeeSkillsScreen
{
    private static readonly string[] Categories = ["BACKEND", "FRONTEND", "DEVOPS", "QA", "OTHER"];
    private static readonly string[] Proficiencies = ["BEGINNER", "INTERMEDIATE", "ADVANCED"];

    public static async Task RunAsync(AppClients clients)
    {
        ConsoleHelper.PrintHeader("Manage Employee/Resource Skills");

        Console.Write("Enter Employee/Resource ID: ");
        if (!long.TryParse(Console.ReadLine()?.Trim(), out var employeeId))
        {
            ConsoleHelper.PrintError("Invalid employee/resource ID.");
            return;
        }

        try
        {
            while (true)
            {
                var detail = await clients.Admin.GetEmployeeAsync(employeeId);
                if (detail is null)
                {
                    ConsoleHelper.PrintError("Employee/Resource not found.");
                    return;
                }

                ConsoleHelper.PrintHeader($"Skills — {detail.FullName} (Employee/Resource)");
                Console.WriteLine("Current Skills:");
                if (detail.Skills.Count == 0)
                    Console.WriteLine("  (none)");
                else
                {
                    for (var i = 0; i < detail.Skills.Count; i++)
                    {
                        var skill = detail.Skills[i];
                        Console.WriteLine($"  {i + 1}.  {skill.SkillName,-18}{FormatProficiency(skill.ProficiencyLevel)}");
                    }
                }

                ConsoleHelper.PrintDivider();
                Console.WriteLine("1. Add Skill");
                Console.WriteLine("2. Update Proficiency Level");
                Console.WriteLine("3. Remove Skill");
                Console.WriteLine("4. Back");
                ConsoleHelper.PrintDivider();
                Console.Write("Enter option: ");
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        await AddSkillAsync(clients, employeeId);
                        break;
                    case "2":
                        await UpdateProficiencyAsync(clients, employeeId, detail.Skills);
                        break;
                    case "3":
                        await RemoveSkillAsync(clients, employeeId, detail.Skills);
                        break;
                    case "4":
                    case "0":
                        return;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            }
        }
        catch (SessionExpiredException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorDisplayHelper.HandleException(ex);
        }
    }

    private static async Task AddSkillAsync(AppClients clients, long employeeId)
    {
        Console.Write("Skill Name        : ");
        var skillName = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.WriteLine("Category          : (1) Backend  (2) Frontend  (3) DevOps  (4) QA  (5) Other");
        Console.Write("Enter choice      : ");
        var category = ParseChoice(Console.ReadLine(), Categories);

        Console.WriteLine("Proficiency Level : (1) Beginner  (2) Intermediate  (3) Advanced");
        Console.Write("Enter choice      : ");
        var proficiency = ParseChoice(Console.ReadLine(), Proficiencies);

        if (string.IsNullOrWhiteSpace(skillName) || category is null || proficiency is null)
        {
            ConsoleHelper.PrintError("All fields are required.");
            return;
        }

        await clients.Admin.AddSkillAsync(employeeId, new AddSkillRequest
        {
            SkillName = skillName,
            Category = category,
            ProficiencyLevel = proficiency
        });

        ConsoleHelper.PrintSuccess("Skill added.");
    }

    private static async Task UpdateProficiencyAsync(AppClients clients, long employeeId, List<EmployeeSkillItem> skills)
    {
        if (skills.Count == 0)
        {
            ConsoleHelper.PrintError("No skills to update.");
            return;
        }

        Console.Write("Skill number to update: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var index) || index < 1 || index > skills.Count)
        {
            ConsoleHelper.PrintError("Invalid skill number.");
            return;
        }

        Console.WriteLine("Proficiency Level : (1) Beginner  (2) Intermediate  (3) Advanced");
        Console.Write("Enter choice      : ");
        var proficiency = ParseChoice(Console.ReadLine(), Proficiencies);
        if (proficiency is null)
        {
            ConsoleHelper.PrintError("Invalid proficiency.");
            return;
        }

        var skill = skills[index - 1];
        await clients.Admin.UpdateSkillProficiencyAsync(
            employeeId,
            skill.SkillId,
            new UpdateSkillProficiencyRequest { ProficiencyLevel = proficiency });

        ConsoleHelper.PrintSuccess("Proficiency updated.");
    }

    private static async Task RemoveSkillAsync(AppClients clients, long employeeId, List<EmployeeSkillItem> skills)
    {
        if (skills.Count == 0)
        {
            ConsoleHelper.PrintError("No skills to remove.");
            return;
        }

        Console.Write("Skill number to remove: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var index) || index < 1 || index > skills.Count)
        {
            ConsoleHelper.PrintError("Invalid skill number.");
            return;
        }

        var skill = skills[index - 1];
        await clients.Admin.DeleteSkillAsync(employeeId, skill.SkillId);
        ConsoleHelper.PrintSuccess("Skill removed.");
    }

    private static string? ParseChoice(string? input, string[] options)
    {
        if (!int.TryParse(input?.Trim(), out var choice) || choice < 1 || choice > options.Length)
            return null;
        return options[choice - 1];
    }

    private static string FormatProficiency(string level) => level switch
    {
        "BEGINNER" => "Beginner",
        "INTERMEDIATE" => "Intermediate",
        "ADVANCED" => "Advanced",
        _ => level
    };
}
