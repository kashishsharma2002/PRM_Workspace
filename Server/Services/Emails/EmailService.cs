using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Server.Common.Emails;
using Server.Models.Emails;
using Server.Models.Entities;
using Server.Repositories.Emails;
using Server.Services.Emails.Providers;
using Server.Services.Emails.Templates;

namespace Server.Services.Emails;

public class EmailService(
    IEmailProvider emailProvider,
    IEmailTemplateRepository emailTemplateRepository,
    IEmailLogRepository emailLogRepository,
    ITemplateRenderingService templateRenderingService,
    ILogger<EmailService> logger) : IEmailService
{
    private static readonly ResiliencePipeline<EmailResult> SendRetryPipeline = new ResiliencePipelineBuilder<EmailResult>()
        .AddRetry(new RetryStrategyOptions<EmailResult>
        {
            MaxRetryAttempts = EmailDefaults.MaxRetryAttempts,
            DelayGenerator = static args => ValueTask.FromResult<TimeSpan?>(
                TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber))),
            ShouldHandle = new PredicateBuilder<EmailResult>()
                .HandleResult(result => !result.Success)
        })
        .Build();

    public async Task<bool> SendNotificationAsync(
        string recipient,
        string emailType,
        Dictionary<string, string> placeholders,
        string? entityReference = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var template = await emailTemplateRepository.GetByKeyAsync(emailType, cancellationToken);
        if (template is null)
        {
            logger.LogWarning("Email template {EmailType} not found.", emailType);
            return false;
        }

        var subject = templateRenderingService.Render(template.SubjectTemplate, placeholders);
        var body = templateRenderingService.Render(template.BodyTemplate, placeholders);
        var resolvedCorrelationId = correlationId ?? Guid.NewGuid().ToString("N");

        var message = new EmailMessage
        {
            Recipient = recipient,
            Subject = subject,
            Body = body,
            EmailType = emailType,
            EntityReference = entityReference,
            CorrelationId = resolvedCorrelationId
        };

        var result = await SendRetryPipeline.ExecuteAsync(
            async ct => await emailProvider.SendEmailAsync(message, ct),
            cancellationToken);

        await emailLogRepository.AddAsync(new EmailLog
        {
            Recipient = recipient,
            EmailType = emailType,
            Subject = subject,
            Status = result.Success ? EmailStatusConstants.Sent : EmailStatusConstants.Failed,
            SentTime = DateTime.UtcNow,
            EntityReference = entityReference,
            ErrorMessage = result.ErrorMessage,
            ProcessingDuration = (long)result.ProcessingDuration.TotalMilliseconds,
            CorrelationId = resolvedCorrelationId
        }, cancellationToken);

        await emailLogRepository.SaveChangesAsync(cancellationToken);

        if (!result.Success)
            logger.LogWarning("Email send failed for {EmailType} to {Recipient}: {Error}", emailType, recipient, result.ErrorMessage);

        return result.Success;
    }
}
