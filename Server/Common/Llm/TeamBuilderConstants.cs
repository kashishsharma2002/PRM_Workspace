namespace Server.Common.Llm;

public static class TeamBuilderConstants
{
    public const string StatusFilled = "FILLED";
    public const string StatusGap = "GAP";

    public const string GapReasonNoSkill = "NO_SKILL";
    public const string GapReasonAllocatedElsewhere = "ALLOCATED_ELSEWHERE";
    public const string GapReasonAlreadyAssignedInTeam = "ALREADY_ASSIGNED_IN_TEAM";
}
