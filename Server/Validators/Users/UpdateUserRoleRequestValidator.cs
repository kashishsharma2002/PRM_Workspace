using FluentValidation;
using Server.Common.Roles;
using Server.Models.DTOs.Users;

namespace Server.Validators.Users;

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequestDto>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => RoleConstants.All.Contains(role.Trim().ToUpperInvariant()))
            .WithMessage("Role must be ADMIN, MANAGER, or EMPLOYEE.");
    }
}
