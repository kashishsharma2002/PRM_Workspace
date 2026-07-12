using Microsoft.Extensions.Options;
using Server.Configuration;
using Server.Models.Emails;

namespace Server.Services.Emails.Infrastructure;

public class EmailConfigResolver(IOptions<SmtpSettingsOptions> smtpOptions)
{
    public Task<SmtpSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var options = smtpOptions.Value;

        if (string.IsNullOrWhiteSpace(options.Host))
            throw new InvalidOperationException("SmtpSettings:Host is not configured in appsettings.json.");

        if (string.IsNullOrWhiteSpace(options.FromEmail))
            throw new InvalidOperationException("SmtpSettings:FromEmail is not configured in appsettings.json.");

        return Task.FromResult(new SmtpSettings
        {
            Host = options.Host,
            Port = options.Port > 0 ? options.Port : 587,
            Username = options.Username,
            Password = options.Password,
            SslEnabled = options.SslEnabled,
            FromEmail = options.FromEmail,
            FromName = string.IsNullOrWhiteSpace(options.FromName) ? "PRM Notifications" : options.FromName
        });
    }
}
