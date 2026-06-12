using FluentValidation;
using Server.Models.DTOs.Allocations;

namespace Server.Validators.Allocations;

public class UpdateAllocationRequestValidator : AbstractValidator<UpdateAllocationRequestDto>
{
    public UpdateAllocationRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.AllocationPercentage.HasValue
                || x.AllocationStartDate.HasValue
                || x.AllocationEndDate.HasValue)
            .WithMessage("At least one allocation field must be provided.");

        RuleFor(x => x.AllocationPercentage)
            .InclusiveBetween(1, 100).When(x => x.AllocationPercentage.HasValue)
            .WithMessage("Allocation percentage must be between 1 and 100.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (!x.AllocationStartDate.HasValue || !x.AllocationEndDate.HasValue)
                    return true;

                return x.AllocationEndDate > x.AllocationStartDate;
            })
            .WithMessage("End date must be after start date.");
    }
}
