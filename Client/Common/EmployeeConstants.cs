namespace Client.Common;

public static class EmployeeConstants
{
    public const string BackendCategory = "BACKEND";
    public const string FrontendCategory = "FRONTEND";
    public const string DevOpsCategory = "DEVOPS";
    public const string QaCategory = "QA";
    public const string OtherCategory = "OTHER";

    public const string ProficiencyBeginner = "BEGINNER";
    public const string ProficiencyIntermediate = "INTERMEDIATE";
    public const string ProficiencyAdvanced = "ADVANCED";

    public static readonly string[] SkillCategories =
    [
        BackendCategory,
        FrontendCategory,
        DevOpsCategory,
        QaCategory,
        OtherCategory
    ];

    public static readonly string[] ProficiencyLevels =
    [
        ProficiencyBeginner,
        ProficiencyIntermediate,
        ProficiencyAdvanced
    ];
}
