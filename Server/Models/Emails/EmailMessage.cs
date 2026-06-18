namespace Server.Models.Emails;

public class EmailMessage
{
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string EmailType { get; set; } = string.Empty;
    public string? EntityReference { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
