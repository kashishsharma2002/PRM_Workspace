using FluentValidation;
using Server.Common;
using Server.Models.DTOs.SystemConfig;

namespace Server.Validators.SystemConfig;

public class UpdateSystemConfigRequestValidator : AbstractValidator<UpdateSystemConfigRequestDto>
{
    public UpdateSystemConfigRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.LlmProvider)
                || !string.IsNullOrWhiteSpace(x.LlmApiKey)
                || x.SchedulerIntervalHours.HasValue
                || x.MaxWeeklyHours.HasValue
                || x.HealthLowHoursThreshold.HasValue
                || x.HealthApproachingDeadlineDays.HasValue)
            .WithMessage("At least one setting must be provided.");

        RuleFor(x => x.LlmProvider)
            .Must(p => p is null
                || p.Equals(LlmProviders.Gemini, StringComparison.OrdinalIgnoreCase)
                || p.Equals(LlmProviders.Groq, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"LLM provider must be {LlmProviders.Gemini} or {LlmProviders.Groq}.");

        RuleFor(x => x.SchedulerIntervalHours)
            .GreaterThan(0).When(x => x.SchedulerIntervalHours.HasValue)
            .WithMessage("Scheduler interval must be greater than 0.");

        RuleFor(x => x.MaxWeeklyHours)
            .GreaterThan(0).When(x => x.MaxWeeklyHours.HasValue)
            .WithMessage("Max weekly hours must be greater than 0.");

        RuleFor(x => x.HealthLowHoursThreshold)
            .InclusiveBetween(0.01m, 1m).When(x => x.HealthLowHoursThreshold.HasValue)
            .WithMessage("Health low-hours threshold must be between 0.01 and 1.");

        RuleFor(x => x.HealthApproachingDeadlineDays)
            .GreaterThan(0).When(x => x.HealthApproachingDeadlineDays.HasValue)
            .WithMessage("Approaching deadline days must be greater than 0.");
    }
}
