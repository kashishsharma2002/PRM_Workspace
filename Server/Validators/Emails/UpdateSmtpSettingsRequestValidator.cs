using FluentValidation;
using Server.Models.DTOs.Emails;

namespace Server.Validators.Emails;

public class UpdateSmtpSettingsRequestValidator : AbstractValidator<UpdateSmtpSettingsRequestDto>
{
    public UpdateSmtpSettingsRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.SmtpHost)
                || x.SmtpPort.HasValue
                || x.SmtpUsername is not null
                || x.SmtpPassword is not null
                || x.SmtpSslEnabled.HasValue
                || !string.IsNullOrWhiteSpace(x.SmtpFromEmail)
                || !string.IsNullOrWhiteSpace(x.SmtpFromName))
            .WithMessage("At least one SMTP setting must be provided.");

        RuleFor(x => x.SmtpPort)
            .InclusiveBetween(1, 65535).When(x => x.SmtpPort.HasValue)
            .WithMessage("SMTP port must be between 1 and 65535.");

        RuleFor(x => x.SmtpFromEmail)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.SmtpFromEmail))
            .WithMessage("SMTP from email must be a valid email address.");
    }
}
