using FluentValidation;
using Server.Common;
using Server.Models.DTOs.Projects;

namespace Server.Validators.Projects;

public class UpdateMilestoneStatusRequestValidator : AbstractValidator<UpdateMilestoneStatusRequestDto>
{
    public UpdateMilestoneStatusRequestValidator()
    {
        RuleFor(x => x.MilestoneStatus)
            .NotEmpty().WithMessage("Milestone status is required.")
            .Must(s => ProjectConstants.MilestoneStatuses.Contains(s.Trim().ToUpperInvariant()))
            .WithMessage("Status must be NOT_STARTED, IN_PROGRESS, or DONE.");
    }
}
