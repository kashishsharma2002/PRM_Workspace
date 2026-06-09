using FluentValidation;
using Server.Common;
using Server.Models.DTOs.Projects;

namespace Server.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequestDto>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.ProjectName)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be on or after start date.");

        RuleFor(x => x.ProjectStatus)
            .NotEmpty().WithMessage("Project status is required.")
            .Must(s => ProjectConstants.CreateStatuses.Contains(s.Trim().ToUpperInvariant()))
            .WithMessage("Status must be PLANNED, ACTIVE, or ON_HOLD.");

        RuleFor(x => x.ManagerUserId)
            .GreaterThan(0).WithMessage("Manager user ID is required.");

        RuleFor(x => x.TotalStoryPoints)
            .GreaterThanOrEqualTo(0).WithMessage("Total story points cannot be negative.");
    }
}
