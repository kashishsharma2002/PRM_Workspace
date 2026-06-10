namespace Server.Common;

public static class TimesheetConstants
{
    public const string StatusSubmitted = "SUBMITTED";
    public const string StatusMissed = "MISSED";

    public const string AllocationStatusActive = "ACTIVE";
    public const string AllocationStatusEnded = "ENDED";

    public const string OtherTagCode = "OTHER";
    public const string ActivityTagsCacheKey = "ActivityTags_All";
    public const int ActivityTagsCacheMinutes = 30;
}
