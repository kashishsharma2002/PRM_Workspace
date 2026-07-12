using Microsoft.Extensions.Options;
using Server.Configuration;
using Server.Services.Emails.Infrastructure;
using Xunit;

namespace Tests.Services.Emails;

public class EmailConfigResolverTests
{
    [Fact]
    public async Task GetSettingsAsync_ReturnsOptionsFromAppSettings()
    {
        var options = Options.Create(new SmtpSettingsOptions
        {
            Host = "smtp.example.com",
            Port = 587,
            Username = "user",
            Password = "secret",
            SslEnabled = false,
            FromEmail = "noreply@example.com",
            FromName = "PRM"
        });

        var resolver = new EmailConfigResolver(options);
        var settings = await resolver.GetSettingsAsync();

        Assert.Equal("smtp.example.com", settings.Host);
        Assert.Equal(587, settings.Port);
        Assert.False(settings.SslEnabled);
        Assert.Equal("secret", settings.Password);
    }

    [Fact]
    public async Task GetSettingsAsync_Throws_WhenHostMissing()
    {
        var resolver = new EmailConfigResolver(Options.Create(new SmtpSettingsOptions
        {
            FromEmail = "noreply@example.com"
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.GetSettingsAsync());
    }
}
