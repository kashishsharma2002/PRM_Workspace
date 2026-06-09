using FluentValidation;
using Server.Models.DTOs.Projects;

namespace Server.Validators;

public class CreateMilestoneRequestValidator : AbstractValidator<CreateMilestoneRequestDto>
{
    public CreateMilestoneRequestValidator()
    {
        RuleFor(x => x.MilestoneTitle)
            .NotEmpty().WithMessage("Milestone title is required.")
            .MaximumLength(200);

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required.");

        RuleFor(x => x.StoryPoints)
            .GreaterThanOrEqualTo(0).WithMessage("Story points cannot be negative.");
    }
}
