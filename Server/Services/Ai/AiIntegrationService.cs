using Microsoft.Extensions.Logging;
using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.Common;
using Server.Common.Ai;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Models.DTOs.Ai;
using Server.Models.DTOs.Ai.Context;
using Server.Models.Entities;
using Server.Repositories.Ai;
using Server.Repositories.Projects;
using Server.Repositories.SystemConfig;
using Server.Services.Ai.Abstractions;

namespace Server.Services.Ai;

public class AiIntegrationService(
    IProjectRepository projectRepository,
    ILlmClientFactory llmClientFactory,
    ISystemConfigRepository systemConfigRepository,
    IAiRequestLogRepository aiRequestLogRepository,
    IAiContextBuilder contextBuilder,
    IAiResponseParser responseParser,
    ITeamBuilderResponseNormalizer teamBuilderResponseNormalizer,
    SkillMatchCandidateFilter skillMatchCandidateFilter,
    SkillMatchRanker skillMatchRanker,
    ILogger<AiIntegrationService> logger) : IAiIntegrationService
{
    public async Task<AiRiskSummaryResponseDto> GetRiskSummaryAsync(
        long managerUserId,
        long projectId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("AI risk summary requested for project {ProjectId} by manager {ManagerUserId}", projectId, managerUserId);

        await EnsureManagerOwnsProjectAsync(managerUserId, projectId, cancellationToken);

        var (_, jsonContext) = await contextBuilder.BuildRiskContextAsync(projectId, cancellationToken);
        var prompt = AiPromptBuilder.BuildRiskSummaryPrompt(projectId, jsonContext);

        var responseText = await GenerateCompletionAsync(prompt, cancellationToken);
        var result = responseParser.ParseRiskSummary(responseText, projectId);

        await LogRequestAsync(AiRequestTypeConstants.RiskSummary, prompt,
            result.Summary.Length > 1000 ? result.Summary[..1000] : result.Summary,
            managerUserId, cancellationToken);

        return result;
    }

    public async Task<AiSkillMatchResponseDto> GetSkillMatchAsync(
        long managerUserId,
        long projectId,
        string? requirement,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "AI skill match requested for project {ProjectId} by manager {ManagerUserId} with requirement: {Requirement}",
            projectId, managerUserId, requirement);

        ValidateRequirement(requirement);
        await EnsureManagerOwnsProjectAsync(managerUserId, projectId, cancellationToken);

        var (context, jsonContext) = await contextBuilder.BuildSkillMatchContextAsync(projectId, cancellationToken);
        var filteredCandidates = skillMatchCandidateFilter.FilterByCandidateSkills(requirement, context.Candidates);

        var prompt = AiPromptBuilder.BuildSkillMatchPrompt(projectId, requirement, jsonContext);

        var responseText = await GenerateCompletionAsync(prompt, cancellationToken);
        var result = responseParser.ParseSkillMatch(responseText, projectId);

        result = skillMatchRanker.RankAndFilterMatches(result, filteredCandidates, requirement);

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

        var (context, jsonContext) = await contextBuilder.BuildOrganizationalSkillMatchContextAsync(cancellationToken);
        var filteredCandidates = skillMatchCandidateFilter.FilterByCandidateSkills(requirement, context.Candidates);

        var prompt = AiPromptBuilder.BuildOrganizationalSkillMatchPrompt(requirement, jsonContext);
        
        var responseText = await GenerateCompletionAsync(prompt, cancellationToken);
        var result = responseParser.ParseSkillMatch(responseText, projectId: 0);

        result = skillMatchRanker.RankAndFilterMatches(result, filteredCandidates, requirement);

        await LogRequestAsync(AiRequestTypeConstants.SkillMatch, prompt,
            $"Generated {result.Matches.Count} organizational matches (filtered and ranked).",
            managerUserId, cancellationToken);

        return result;
    }

    public async Task<TeamBuilderResponseDto> BuildTeamAsync(
        long managerUserId,
        string? requirement,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "AI team builder requested by manager {ManagerUserId} with requirement: {Requirement}",
            managerUserId, requirement);

        var rawContext = await contextBuilder.BuildTeamBuilderRawContextAsync(requirement!, cancellationToken);
        ApplyAssignablePool(rawContext);

        var jsonContext = contextBuilder.SerializeTeamBuilderContext(rawContext);
        var prompt = AiPromptBuilder.BuildTeamBuilderPrompt(requirement!, jsonContext);

        var responseText = await GenerateCompletionAsync(prompt, cancellationToken);
        TeamBuilderResponseDto parsed;
        try
        {
            parsed = responseParser.ParseTeamBuilder(responseText);
        }
        catch (ValidationAppException ex) when (ex.ErrorCode == ErrorCodes.LlmResponseInvalid)
        {
            logger.LogWarning("Team builder initial parse failed for manager {ManagerUserId}, retrying with repair prompt", managerUserId);
            var repairPrompt = AiPromptBuilder.BuildTeamBuilderRepairPrompt(requirement!, responseText);
            responseText = await GenerateCompletionAsync(repairPrompt, cancellationToken);
            parsed = responseParser.ParseTeamBuilder(responseText);
        }

        var result = teamBuilderResponseNormalizer.Normalize(
            parsed, rawContext.AssignableCandidates, rawContext.AllCandidates);

        var filledCount = result.Roles.Count(r =>
            string.Equals(r.Status, TeamBuilderConstants.StatusFilled, StringComparison.OrdinalIgnoreCase));
        var gapCount = result.Roles.Count - filledCount;

        logger.LogDebug(
            "AI team builder completed for manager {ManagerUserId}: {RoleCount} roles, {FilledCount} filled, {GapCount} gaps",
            managerUserId, result.Roles.Count, filledCount, gapCount);

        await LogRequestAsync(AiRequestTypeConstants.TeamBuilder, prompt,
            $"Generated {result.Roles.Count} roles ({filledCount} filled, {gapCount} gaps).",
            managerUserId, cancellationToken);

        return result;
    }

    private static void ApplyAssignablePool(AiTeamBuilderContextModel context)
    {
        context.AssignableCandidates = context.AllCandidates
            .Where(c => c.RemainingCapacityPercentage == AllocationConstants.MaxUtilizationPercentage)
            .ToList();
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
