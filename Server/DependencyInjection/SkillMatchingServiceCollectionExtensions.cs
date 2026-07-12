using FluentValidation;
using Server.Services.SkillMatching;
using Server.Services.SkillMatching.Abstractions;
using Server.Validators.SkillMatching;

namespace Server.DependencyInjection;

public static class SkillMatchingServiceCollectionExtensions
{
    public static IServiceCollection AddSkillMatching(this IServiceCollection services)
    {
        services.AddScoped<ISkillMatchPromptBuilder, SkillMatchPromptBuilder>();
        services.AddScoped<ISkillMatchResponseParser, SkillMatchResponseParser>();
        services.AddScoped<ISkillMatchContextAssembler, SkillMatchContextAssembler>();
        services.AddScoped<ISkillMatchCandidateFilter, SkillMatchCandidateFilter>();
        services.AddScoped<ISkillMatchRanker, SkillMatchRanker>();
        services.AddScoped<IProjectHealthResourceFilter, ProjectHealthResourceFilter>();
        services.AddScoped<ISkillMatchingService, SkillMatchingService>();

        services.AddScoped<IValidator<SkillMatchQuery>, SkillMatchQueryValidator>();

        return services;
    }
}
