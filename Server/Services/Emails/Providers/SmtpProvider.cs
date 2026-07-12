using System.Diagnostics;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Server.Models.Emails;
using Server.Services.Emails.Infrastructure;

namespace Server.Services.Emails.Providers;

public class SmtpProvider(EmailConfigResolver configResolver) : IEmailProvider
{
    public async Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var settings = await configResolver.GetSettingsAsync(cancellationToken);
            var mimeMessage = BuildMimeMessage(settings, message);

            await SendWithClientAsync(settings, mimeMessage, cancellationToken);

            stopwatch.Stop();
            return new EmailResult
            {
                Success = true,
                ProcessingDuration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingDuration = stopwatch.Elapsed
            };
        }
    }

    public async Task<EmailResult> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var settings = await configResolver.GetSettingsAsync(cancellationToken);
            using var client = new SmtpClient();
            await ConnectAndAuthenticateAsync(client, settings, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            stopwatch.Stop();
            return new EmailResult
            {
                Success = true,
                ProcessingDuration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingDuration = stopwatch.Elapsed
            };
        }
    }

    private static MimeMessage BuildMimeMessage(SmtpSettings settings, EmailMessage message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        mimeMessage.To.Add(new MailboxAddress(string.Empty, message.Recipient));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = message.Body };
        mimeMessage.Body = bodyBuilder.ToMessageBody();
        mimeMessage.Headers.Add("X-Correlation-ID", message.CorrelationId);

        return mimeMessage;
    }

    private static async Task SendWithClientAsync(
        SmtpSettings settings,
        MimeMessage mimeMessage,
        CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();
        await ConnectAndAuthenticateAsync(client, settings, cancellationToken);
        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static async Task ConnectAndAuthenticateAsync(
        SmtpClient client,
        SmtpSettings settings,
        CancellationToken cancellationToken)
    {
        var socketOptions = settings.SslEnabled ? SecureSocketOptions.Auto : SecureSocketOptions.None;
        await client.ConnectAsync(settings.Host, settings.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
    }
}
