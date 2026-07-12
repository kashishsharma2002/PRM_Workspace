using Server.Models.Emails;

namespace Server.Services.Emails.Providers;

public interface IEmailProvider
{
    Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<EmailResult> ValidateConnectionAsync(CancellationToken cancellationToken = default);
}
