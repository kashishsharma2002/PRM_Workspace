using FluentValidation;
using Server.Common;
using Server.Models.DTOs.Employees;

namespace Server.Validators;

public class UpdateSkillProficiencyRequestValidator : AbstractValidator<UpdateSkillProficiencyRequestDto>
{
    public UpdateSkillProficiencyRequestValidator()
    {
        RuleFor(x => x.ProficiencyLevel)
            .NotEmpty().WithMessage("Proficiency level is required.")
            .Must(p => EmployeeConstants.ProficiencyLevels.Contains(p.Trim().ToUpperInvariant()))
            .WithMessage("Proficiency must be BEGINNER, INTERMEDIATE, or ADVANCED.");
    }
}
