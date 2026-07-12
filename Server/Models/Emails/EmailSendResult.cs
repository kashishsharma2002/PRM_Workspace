namespace Server.Models.Emails;

public class EmailSendResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public TimeSpan ProcessingDuration { get; set; }
}
