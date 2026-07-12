using System.Text.Json;
using Microsoft.Extensions.Logging;
using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.Common;
using Server.Common.Errors;
using Server.Common.Llm;
using Server.Exceptions;
using Server.Models.DTOs.ProjectRisk;
using Server.Models.Entities;
using Server.Repositories.AiRequestLog;
using Server.Repositories.Projects;
using Server.Repositories.SystemConfig;
using Server.Services.ProjectRisk.Abstractions;

namespace Server.Services.ProjectRisk;

public class ProjectRiskInsightsService(
    IProjectRepository projectRepository,
    ILlmClientFactory llmClientFactory,
    ISystemConfigRepository systemConfigRepository,
    IAiRequestLogRepository aiRequestLogRepository,
    IProjectRiskContextAssembler contextAssembler,
    IProjectRiskPromptBuilder promptBuilder,
    IProjectRiskResponseParser responseParser,
    ILogger<ProjectRiskInsightsService> logger) : IProjectRiskInsightsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<AiRiskSummaryResponseDto> GetRiskSummaryAsync(
        long managerUserId,
        long projectId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("AI risk summary requested for project {ProjectId} by manager {ManagerUserId}", projectId, managerUserId);

        await EnsureManagerOwnsProjectAsync(managerUserId, projectId, cancellationToken);

        var context = await contextAssembler.BuildAsync(projectId, cancellationToken);
        var jsonContext = JsonSerializer.Serialize(context, JsonOptions);
        var prompt = promptBuilder.BuildRiskSummaryPrompt(projectId, jsonContext);

        var responseText = await GenerateCompletionAsync(prompt, cancellationToken);
        var result = responseParser.ParseRiskSummary(responseText, projectId);

        await LogRequestAsync(AiRequestTypeConstants.RiskSummary, prompt,
            result.Summary.Length > AiValidationLimits.MaxResponseSummaryLength
                ? result.Summary[..AiValidationLimits.MaxResponseSummaryLength]
                : result.Summary,
            managerUserId, cancellationToken);

        return result;
    }

    private async Task EnsureManagerOwnsProjectAsync(
        long managerUserId,
        long projectId,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundAppException("Project not found.", ErrorCodes.ProjectNotFound);

        if (project.ManagerUserId != managerUserId)
            throw new NotFoundAppException("Project not found.", ErrorCodes.ProjectNotFound);
    }

    private async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken)
    {
        var providerConfig = await systemConfigRepository.GetByKeyAsync(ConfigKeys.LlmProvider, cancellationToken);
        var activeProvider = string.IsNullOrWhiteSpace(providerConfig?.ConfigValue)
            ? LlmProviderKeys.Gemini
            : providerConfig.ConfigValue.Trim();

        var client = llmClientFactory.CreateClient(activeProvider);
        return await client.GenerateCompletionAsync(prompt, cancellationToken);
    }

    private async Task LogRequestAsync(
        string requestType,
        string prompt,
        string responseSummary,
        long managerUserId,
        CancellationToken cancellationToken)
    {
        var log = new AiRequestLog
        {
            RequestType = requestType,
            Prompt = prompt,
            ResponseSummary = responseSummary,
            RequestedByUserId = managerUserId,
            CreatedAt = DateTime.UtcNow
        };

        await aiRequestLogRepository.AddAsync(log, cancellationToken);
        await aiRequestLogRepository.SaveChangesAsync(cancellationToken);
    }
}
