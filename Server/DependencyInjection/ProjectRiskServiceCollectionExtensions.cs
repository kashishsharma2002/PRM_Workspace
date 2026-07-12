using Server.Services.ProjectRisk;
using Server.Services.ProjectRisk.Abstractions;

namespace Server.DependencyInjection;

public static class ProjectRiskServiceCollectionExtensions
{
    public static IServiceCollection AddProjectRisk(this IServiceCollection services)
    {
        services.AddScoped<IProjectRiskContextAssembler, ProjectRiskContextAssembler>();
        services.AddScoped<IProjectRiskPromptBuilder, ProjectRiskPromptBuilder>();
        services.AddScoped<IProjectRiskResponseParser, ProjectRiskResponseParser>();
        services.AddScoped<IProjectRiskInsightsService, ProjectRiskInsightsService>();

        return services;
    }
}
