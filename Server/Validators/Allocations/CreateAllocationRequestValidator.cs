using FluentValidation;
using Server.Models.DTOs.Allocations;

namespace Server.Validators.Allocations;

public class CreateAllocationRequestValidator : AbstractValidator<CreateAllocationRequestDto>
{
    public CreateAllocationRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("Employee ID is required.");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("Project ID is required.");

        RuleFor(x => x.AllocationPercentage)
            .InclusiveBetween(1, 100).WithMessage("Allocation percentage must be between 1 and 100.");

        RuleFor(x => x.AllocationStartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.AllocationEndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.AllocationStartDate).WithMessage("End date must be after start date.");
    }
}
