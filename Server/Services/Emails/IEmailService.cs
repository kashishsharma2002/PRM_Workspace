namespace Server.Services.Emails;

public interface IEmailService
{
    Task<bool> SendNotificationAsync(
        string recipient,
        string emailType,
        Dictionary<string, string> placeholders,
        string? entityReference = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
