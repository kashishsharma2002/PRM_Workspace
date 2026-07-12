using Server.Services.Projects;
using Server.Services.Projects.Abstractions;
using Server.Services.Projects.Rules;

namespace Server.DependencyInjection;

public static class ProjectHealthServiceCollectionExtensions
{
    public static IServiceCollection AddProjectHealth(this IServiceCollection services)
    {
        services.AddScoped<IProjectHealthFlagRule, OverdueMilestoneFlagRule>();
        services.AddScoped<IProjectHealthFlagRule, LowHoursFlagRule>();
        services.AddScoped<IProjectHealthFlagRule, ApproachingDeadlineFlagRule>();
        services.AddScoped<IProjectHealthFlagEvaluator, ProjectHealthFlagEvaluator>();

        return services;
    }
}
