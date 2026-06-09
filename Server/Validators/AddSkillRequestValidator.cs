using FluentValidation;
using Server.Common;
using Server.Models.DTOs.Employees;

namespace Server.Validators;

public class AddSkillRequestValidator : AbstractValidator<AddSkillRequestDto>
{
    public AddSkillRequestValidator()
    {
        RuleFor(x => x.SkillName)
            .NotEmpty().WithMessage("Skill name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .Must(c => EmployeeConstants.SkillCategories.Contains(c.Trim().ToUpperInvariant()))
            .WithMessage("Category must be BACKEND, FRONTEND, DEVOPS, QA, or OTHER.");

        RuleFor(x => x.ProficiencyLevel)
            .NotEmpty().WithMessage("Proficiency level is required.")
            .Must(p => EmployeeConstants.ProficiencyLevels.Contains(p.Trim().ToUpperInvariant()))
            .WithMessage("Proficiency must be BEGINNER, INTERMEDIATE, or ADVANCED.");
    }
}
