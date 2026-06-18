namespace Server.Common.Emails;

public static class EmailDefaults
{
    public const string MaskedPassword = "********";
    public const int MaxRetryAttempts = 3;
    public const int RecentLogCount = 10;

    /// <summary>Working days after week end (Sunday) when the submission deadline falls.</summary>
    public const int TimesheetDeadlineWorkingDaysAfterWeekEnd = 1;

    public const int DefaultSmtpPort = 587;
    public const string DefaultFromName = "PRM Notifications";
}
