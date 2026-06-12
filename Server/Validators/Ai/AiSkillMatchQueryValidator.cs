using FluentValidation;
using Server.Common.Ai;

namespace Server.Validators.Ai;

public class AiSkillMatchQuery
{
    public string? Requirement { get; set; }
}

public class AiSkillMatchQueryValidator : AbstractValidator<AiSkillMatchQuery>
{
    public AiSkillMatchQueryValidator()
    {
        RuleFor(x => x.Requirement)
            .MaximumLength(AiValidationLimits.MaxRequirementLength)
            .WithMessage($"Requirement must not exceed {AiValidationLimits.MaxRequirementLength} characters.")
            .When(x => x.Requirement is not null);
    }
}
