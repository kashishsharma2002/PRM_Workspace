namespace Server.Models.Emails;

public class EmailResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
}
