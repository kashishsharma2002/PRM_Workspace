using FluentValidation;
using Server.Common.Llm;

namespace Server.Validators.SkillMatching;

public class SkillMatchQuery
{
    public string? Requirement { get; set; }
}

public class SkillMatchQueryValidator : AbstractValidator<SkillMatchQuery>
{
    public SkillMatchQueryValidator()
    {
        RuleFor(x => x.Requirement)
            .MaximumLength(AiValidationLimits.MaxRequirementLength)
            .WithMessage($"Requirement must not exceed {AiValidationLimits.MaxRequirementLength} characters.")
            .When(x => x.Requirement is not null);
    }
}
