using FluentValidation;
using Server.Common;
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
            .Must(d => DepartmentConstants.EmployeeOptions.Contains(d!.Trim().ToUpperInvariant()))
            .WithMessage("Invalid department.")
            .When(x => !string.IsNullOrWhiteSpace(x.Department));

        RuleFor(x => x.Designation)
            .Must(d => DesignationConstants.EmployeeOptions.Contains(d!.Trim().ToUpperInvariant()))
            .WithMessage("Invalid designation.")
            .When(x => !string.IsNullOrWhiteSpace(x.Designation));
    }
}
