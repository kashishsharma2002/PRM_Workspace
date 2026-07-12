namespace Server.Models.DTOs.Emails;

public class EmailLogItemDto
{
    public long Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string EmailType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SentTime { get; set; }
    public string? EntityReference { get; set; }
    public string? ErrorMessage { get; set; }
    public long ProcessingDuration { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
