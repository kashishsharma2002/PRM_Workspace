using FluentValidation;
using Server.Common.Llm;

namespace Server.Validators.TeamBuilder;

public class TeamBuilderQuery
{
    public string? Requirement { get; set; }
}

public class TeamBuilderQueryValidator : AbstractValidator<TeamBuilderQuery>
{
    public TeamBuilderQueryValidator()
    {
        RuleFor(x => x.Requirement)
            .NotEmpty()
            .WithMessage("Requirement description cannot be empty.")
            .MaximumLength(AiValidationLimits.MaxTeamBuilderRequirementLength)
            .WithMessage($"Requirement must not exceed {AiValidationLimits.MaxTeamBuilderRequirementLength} characters.");
    }
}
