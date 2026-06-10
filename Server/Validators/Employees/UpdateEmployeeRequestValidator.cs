using FluentValidation;
using Server.Models.DTOs.Employees;

namespace Server.Validators.Employees;

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequestDto>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Department) || !string.IsNullOrWhiteSpace(x.Designation))
            .WithMessage("At least one of department or designation must be provided.");

        RuleFor(x => x.Department)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Department));

        RuleFor(x => x.Designation)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Designation));
    }
}
