namespace Server.Common.Llm;

public static class AiValidationLimits
{
    public const int MaxRequirementLength = 500;
    public const int MaxTeamBuilderRequirementLength = 1000;
    public const int MaxResponseSummaryLength = 1000;
    public const int RecentTimesheetWindowDays = 28;
}
