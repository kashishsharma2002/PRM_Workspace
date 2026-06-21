namespace Server.Common;

public static class EmployeeConstants
{
    public const string BackendCategory = "BACKEND";
    public const string FrontendCategory = "FRONTEND";
    public const string DevOpsCategory = "DEVOPS";
    public const string QaCategory = "QA";
    public const string OtherCategory = "OTHER";

    public static readonly string[] SkillCategories =
    [
        BackendCategory,
        FrontendCategory,
        DevOpsCategory,
        QaCategory,
        OtherCategory
    ];

    public static readonly string[] ProficiencyLevels = ["BEGINNER", "INTERMEDIATE", "ADVANCED"];
}
