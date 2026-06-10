using FluentValidation;
using Server.Common;
using Server.Models.DTOs.Projects;

namespace Server.Validators.Projects;

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequestDto>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.ProjectName)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be on or after start date.");

        RuleFor(x => x.ProjectStatus)
            .NotEmpty().WithMessage("Project status is required.")
            .Must(s => ProjectConstants.UpdateStatuses.Contains(s.Trim().ToUpperInvariant()))
            .WithMessage("Status must be PLANNED, ACTIVE, ON_HOLD, or COMPLETED.");

        RuleFor(x => x.ManagerUserId)
            .GreaterThan(0).WithMessage("Manager user ID is required.");

        RuleFor(x => x.TotalStoryPoints)
            .GreaterThanOrEqualTo(0).WithMessage("Total story points cannot be negative.");
    }
}
