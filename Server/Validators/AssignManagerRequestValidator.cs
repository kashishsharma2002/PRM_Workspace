using FluentValidation;
using Server.Models.DTOs.Employees;

namespace Server.Validators;

public class AssignManagerRequestValidator : AbstractValidator<AssignManagerRequestDto>
{
    public AssignManagerRequestValidator()
    {
        RuleFor(x => x.ManagerUserId)
            .GreaterThan(0).WithMessage("Manager user ID is required.");
    }
}
