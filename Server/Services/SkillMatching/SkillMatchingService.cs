using System.Text.Json;
using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Errors;
using Server.Common.Llm;
using Server.Exceptions;
using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.Models.DTOs.SkillMatching;
using Server.Models.DTOs.SkillMatching.Context;
using Server.Models.Entities;
using Server.Repositories.AiRequestLog;
using Server.Repositories.Projects;
using Server.Repositories.SystemConfig;
using Server.Services.SkillMatching.Abstractions;

namespace Server.Services.SkillMatching;

public class SkillMatchingService(
    IProjectRepository projectRepository,
    ILlmClientFactory llmClientFactory,
    ISystemConfigRepository systemConfigRepository,
    IAiRequestLogRepository aiRequestLogRepository,
    ISkillMatchContextAssembler skillMatchContextAssembler,
    ISkillMatchResponseParser responseParser,
    ISkillMatchPromptBuilder promptBuilder,
    ISkillMatchCandidateFilter skillMatchCandidateFilter,
    ISkillMatchRanker skillMatchRanker,
    IProjectHealthResourceFilter projectHealthResourceFilter,
    ILogger<SkillMatchingService> logger) : ISkillMatchingService
{
    private static readonly JsonSerializerOptions SkillMatchJsonOptions = new() { WriteIndented = true };

    public async Task<AiSkillMatchResponseDto> GetSkillMatchAsync(
        long managerUserId,
        long projectId,
        string? requirement,
        SkillMatchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "AI skill match requested for project {ProjectId} by manager {ManagerUserId} with requirement: {Requirement}",
            projectId, managerUserId, requirement);

        ValidateRequirement(requirement);
        await EnsureManagerOwnsProjectAsync(managerUserId, projectId, cancellationToken);

        var context = await BuildProjectSkillMatchContextAsync(projectId, cancellationToken);

        List<AiSkillMatchCandidateContext> candidatePool;
        string jsonContext;
        string prompt;

        if (options?.ExcludeAllocatedToProjectId is long excludeProjectId)
        {
            candidatePool = projectHealthResourceFilter.FilterForAtRiskEmail(context.Candidates, excludeProjectId);
            if (candidatePool.Count == 0)
            {
                return new AiSkillMatchResponseDto { ProjectId = projectId, Matches = [] };
            }

            var filteredContext = new AiSkillMatchContextModel
            {
                Project = context.Project,
                Candidates = candidatePool
            };
            jsonContext = JsonSerializer.Serialize(filteredContext, SkillMatchJsonOptions);
            prompt = promptBuilder.BuildAtRiskSkillMatchPrompt(projectId, requirement, jsonContext);
        }
        else
        {
            candidatePool = skillMatchCandidateFilter.FilterByCandidateSkills(requirement, context.Candidates);
            var filteredContext = new AiSkillMatchContextModel
            {
                Project = context.Project,
                Candidates = candidatePool
            };
            jsonContext = JsonSerializer.Serialize(filteredContext, SkillMatchJsonOptions);
            prompt = promptBuilder.BuildSkillMatchPrompt(projectId, requirement, jsonContext);
        }

        var responseText = await GenerateCompletionAsync(prompt, cancellationToken);
        var result = responseParser.ParseSkillMatch(responseText, projectId);

        result = skillMatchRanker.RankAndFilterMatches(result, context.Candidates, requirement);

        await LogRequestAsync(AiRequestTypeConstants.SkillMatch, prompt,
            $"Generated {result.Matches.Count} matches (filtered and ranked).",
            managerUserId, cancellationToken);

        return result;
    }

    public async Task<AiSkillMatchResponseDto> GetOrganizationalSkillMatchAsync(
        long managerUserId,
        string? requirement,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Organizational AI skill match requested by manager {ManagerUserId} with requirement: {Requirement}",
            managerUserId, requirement);

        ValidateRequirement(requirement);

        var candidates = await skillMatchContextAssembler.AssembleCandidatesAsync(cancellationToken);
        var context = new AiOrganizationalSkillMatchContextModel { Candidates = candidates };

        var filteredCandidates = skillMatchCandidateFilter.FilterByCandidateSkills(requirement, context.Candidates);
        var filteredContext = new AiOrganizationalSkillMatchContextModel
        {
            Candidates = filteredCandidates
        };
        var filteredJsonContext = JsonSerializer.Serialize(filteredContext, SkillMatchJsonOptions);

        var prompt = promptBuilder.BuildOrganizationalSkillMatchPrompt(requirement, filteredJsonContext);

        var responseText = await GenerateCompletionAsync(prompt, cancellationToken);
        var result = responseParser.ParseSkillMatch(responseText, projectId: 0);

        result = skillMatchRanker.RankAndFilterMatches(result, context.Candidates, requirement);

        await LogRequestAsync(AiRequestTypeConstants.SkillMatch, prompt,
            $"Generated {result.Matches.Count} organizational matches (filtered and ranked).",
            managerUserId, cancellationToken);

        return result;
    }

    private async Task<AiSkillMatchContextModel> BuildProjectSkillMatchContextAsync(
        long projectId,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project with ID {projectId} not found.");

        var candidates = await skillMatchContextAssembler.AssembleCandidatesAsync(cancellationToken);

        return new AiSkillMatchContextModel
        {
            Project = new AiProjectSummaryContext
            {
                ProjectId = project.Id,
                ProjectName = project.ProjectName,
                Description = project.Description
            },
            Candidates = candidates
        };
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

    private static void ValidateRequirement(string? requirement)
    {
        if (requirement is not null && requirement.Length > AiValidationLimits.MaxRequirementLength)
            throw new ValidationAppException(
                $"Requirement must not exceed {AiValidationLimits.MaxRequirementLength} characters.");
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
