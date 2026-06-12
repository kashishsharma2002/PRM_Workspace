using FluentValidation;
using Server.Models.DTOs.Timesheets;

namespace Server.Validators.Timesheets;

public class TimesheetSubmitRequestValidator : AbstractValidator<TimesheetSubmitRequestDto>
{
    public TimesheetSubmitRequestValidator()
    {
        RuleFor(x => x.WeekStartDate)
            .Must(d => d.DayOfWeek == DayOfWeek.Monday)
            .WithMessage("Week start date must be a Monday.");

        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("At least one line item is required.");

        RuleForEach(x => x.LineItems).ChildRules(item =>
        {
            item.RuleFor(x => x.ProjectId)
                .GreaterThan(0).WithMessage("Project ID is required.");

            item.RuleFor(x => x.HoursLogged)
                .GreaterThan(0).WithMessage("Hours logged must be greater than zero.")
                .LessThanOrEqualTo(40).WithMessage("Hours logged cannot exceed 40 per project.");

            item.RuleFor(x => x.ActivityTagIds)
                .NotEmpty().WithMessage("At least one activity tag is required.");
        });

        RuleFor(x => x.LineItems)
            .Must(items => items.Select(i => i.ProjectId).Distinct().Count() == items.Count)
            .WithMessage("Duplicate project entries are not allowed in the same timesheet.");
    }
}
