using FluentValidation;
using Server.Models.DTOs.Emails;

namespace Server.Validators.Emails;

public class SendTestEmailRequestValidator : AbstractValidator<SendTestEmailRequestDto>
{
    public SendTestEmailRequestValidator()
    {
        RuleFor(x => x.RecipientAddress)
            .NotEmpty().WithMessage("Recipient address is required.")
            .EmailAddress().WithMessage("Recipient address must be a valid email address.");
    }
}
