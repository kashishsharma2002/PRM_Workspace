using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.AI.Infrastructure;
using Server.AI.Providers;
using Server.Repositories.AiRequestLog;
using Server.Services.TeamBuilder;
using Server.Services.TeamBuilder.Abstractions;

namespace Server.DependencyInjection;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddPrmAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LlmSettings>(configuration.GetSection("LlmSettings"));

        services.AddScoped<ILlmConfigResolver, LlmConfigResolver>();
        services.AddScoped<ILlmApiKeyResolver, LlmApiKeyResolver>();
        services.AddScoped<ILlmClient, GeminiClient>();
        services.AddScoped<ILlmClient, GroqClient>();
        services.AddScoped<ILlmClient, GemmaClient>();
        services.AddScoped<ILlmClientFactory, LlmClientFactory>();

        var llmSettings = configuration.GetSection("LlmSettings").Get<LlmSettings>()
            ?? throw new InvalidOperationException("LlmSettings configuration is missing.");
        var httpTimeout = TimeSpan.FromSeconds(llmSettings.HttpTimeoutSeconds);

        services.AddHttpClient("GeminiClient", client =>
        {
            client.BaseAddress = new Uri(llmSettings.Gemini.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = httpTimeout;
        });
        services.AddHttpClient("GroqClient", client =>
        {
            client.BaseAddress = new Uri(llmSettings.Groq.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = httpTimeout;
        });
        services.AddHttpClient("GemmaClient", client =>
        {
            client.Timeout = httpTimeout;
        });

        services.AddSkillMatching();
        services.AddProjectRisk();

        services.AddScoped<IAiRequestLogRepository, AiRequestLogRepository>();
        services.AddScoped<ITeamBuilderContextBuilder, TeamBuilderContextBuilder>();
        services.AddScoped<ITeamBuilderResponseParser, TeamBuilderResponseParser>();
        services.AddScoped<ITeamBuilderPromptBuilder, TeamBuilderPromptBuilder>();
        services.AddScoped<ITeamBuilderResponseNormalizer, TeamBuilderResponseNormalizer>();
        services.AddScoped<ITeamBuilderService, TeamBuilderService>();

        return services;
    }
}
