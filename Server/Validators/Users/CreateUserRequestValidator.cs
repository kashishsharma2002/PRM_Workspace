using FluentValidation;
using Server.Common;
using Server.Common.Roles;
using Server.Models.DTOs.Users;

namespace Server.Validators.Users;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequestDto>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(255);

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100);

        RuleFor(x => x.TemporaryPassword)
            .NotEmpty().WithMessage("Temporary password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(r => RoleConstants.All.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be ADMIN, MANAGER, or EMPLOYEE.");

        RuleFor(x => x.Department)
            .Must((dto, dept) => IsValidDepartment(dto.Role, dept))
            .WithMessage("Invalid department for the selected role.");

        RuleFor(x => x.Designation)
            .Must((dto, desig) => IsValidDesignation(dto.Role, desig))
            .WithMessage("Invalid designation for the selected role.");
    }

    private static bool IsValidDepartment(string role, string? department)
    {
        var normalizedRole = role.Trim().ToUpperInvariant();
        var options = normalizedRole switch
        {
            RoleConstants.Admin => DepartmentConstants.AdminOptions,
            RoleConstants.Manager => DepartmentConstants.ManagerOptions,
            RoleConstants.Employee => DepartmentConstants.EmployeeOptions,
            _ => Array.Empty<string>()
        };

        if (normalizedRole == RoleConstants.Admin)
            return string.IsNullOrWhiteSpace(department)
                || options.Contains(department.Trim().ToUpperInvariant());

        return !string.IsNullOrWhiteSpace(department)
            && options.Contains(department.Trim().ToUpperInvariant());
    }

    private static bool IsValidDesignation(string role, string? designation)
    {
        var normalizedRole = role.Trim().ToUpperInvariant();
        var options = normalizedRole switch
        {
            RoleConstants.Admin => DesignationConstants.AdminOptions,
            RoleConstants.Manager => DesignationConstants.ManagerOptions,
            RoleConstants.Employee => DesignationConstants.EmployeeOptions,
            _ => Array.Empty<string>()
        };

        if (normalizedRole == RoleConstants.Admin)
            return string.IsNullOrWhiteSpace(designation)
                || options.Contains(designation.Trim().ToUpperInvariant());

        return !string.IsNullOrWhiteSpace(designation)
            && options.Contains(designation.Trim().ToUpperInvariant());
    }
}
