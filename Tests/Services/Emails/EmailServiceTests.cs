using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Emails;
using Server.Models.Emails;
using Server.Models.Entities;
using Server.Repositories.Emails;
using Server.Services.Emails;
using Server.Services.Emails.Providers;
using Server.Services.Emails.Templates;
using Xunit;

namespace Tests.Services.Emails;

public class EmailServiceTests
{
    private readonly Mock<IEmailProvider> _providerMock;
    private readonly Mock<IEmailTemplateRepository> _templateRepoMock;
    private readonly Mock<IEmailLogRepository> _logRepoMock;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _providerMock = new Mock<IEmailProvider>();
        _templateRepoMock = new Mock<IEmailTemplateRepository>();
        _logRepoMock = new Mock<IEmailLogRepository>();

        _emailService = new EmailService(
            _providerMock.Object,
            _templateRepoMock.Object,
            _logRepoMock.Object,
            new TemplateRenderingService(),
            new Mock<ILogger<EmailService>>().Object);
    }

    [Fact]
    public async Task SendNotificationAsync_RetriesOnFailure_AndLogsFinalOutcome()
    {
        _templateRepoMock.Setup(r => r.GetByKeyAsync(EmailTypeConstants.TimesheetReminder1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailTemplate
            {
                TemplateKey = EmailTypeConstants.TimesheetReminder1,
                SubjectTemplate = "Reminder {{WeekEndDate}}",
                BodyTemplate = "Dear {{EmployeeName}}"
            });

        var attempts = 0;
        _providerMock.Setup(p => p.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                attempts++;
                return new EmailResult
                {
                    Success = attempts >= 2,
                    ProcessingDuration = TimeSpan.FromMilliseconds(10)
                };
            });

        var success = await _emailService.SendNotificationAsync(
            "user@example.com",
            EmailTypeConstants.TimesheetReminder1,
            new Dictionary<string, string>
            {
                ["EmployeeName"] = "Test User",
                ["WeekEndDate"] = "2026-06-08"
            },
            "WeekEnding:2026-06-08");

        Assert.True(success);
        Assert.True(attempts >= 2);
        _logRepoMock.Verify(r => r.AddAsync(It.Is<EmailLog>(l => l.Status == EmailStatusConstants.Sent), It.IsAny<CancellationToken>()), Times.Once);
        _logRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
