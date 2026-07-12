using Server.Models.Emails;

namespace Server.Services.Emails;

public interface IEmailService
{
    /// <summary>
    /// Sends an email with explicit subject and body. Use this from anywhere in the application.
    /// </summary>
    Task<EmailSendResult> SendEmailAsync(
        string recipient,
        string subject,
        string body,
        string? emailType = null,
        string? entityReference = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a stored template and sends the resulting email.
    /// </summary>
    Task<EmailSendResult> SendTemplatedEmailAsync(
        string recipient,
        string emailType,
        Dictionary<string, string> placeholders,
        string? entityReference = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
