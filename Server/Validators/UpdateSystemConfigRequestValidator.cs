using FluentValidation;
using Server.Models.DTOs.SystemConfig;

namespace Server.Validators;

public class UpdateSystemConfigRequestValidator : AbstractValidator<UpdateSystemConfigRequestDto>
{
    public UpdateSystemConfigRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.LlmProvider)
                || !string.IsNullOrWhiteSpace(x.LlmApiKey)
                || x.SchedulerIntervalHours.HasValue
                || x.MaxWeeklyHours.HasValue)
            .WithMessage("At least one setting must be provided.");

        RuleFor(x => x.LlmProvider)
            .Must(p => p is null || p.Equals("Gemini", StringComparison.OrdinalIgnoreCase) || p.Equals("Groq", StringComparison.OrdinalIgnoreCase))
            .WithMessage("LLM provider must be Gemini or Groq.");

        RuleFor(x => x.SchedulerIntervalHours)
            .GreaterThan(0).When(x => x.SchedulerIntervalHours.HasValue)
            .WithMessage("Scheduler interval must be greater than 0.");

        RuleFor(x => x.MaxWeeklyHours)
            .GreaterThan(0).When(x => x.MaxWeeklyHours.HasValue)
            .WithMessage("Max weekly hours must be greater than 0.");
    }
}
