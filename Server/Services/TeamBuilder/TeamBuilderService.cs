using Microsoft.Extensions.Logging;
using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.Common;
using Server.Common.Llm;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Models.DTOs.Ai;
using Server.Models.DTOs.Ai.Context;
using Server.Models.Entities;
using Server.Repositories.AiRequestLog;
using Server.Repositories.SystemConfig;
using Server.Services.TeamBuilder.Abstractions;

namespace Server.Services.TeamBuilder;

public class TeamBuilderService(
    ILlmClientFactory llmClientFactory,
    ISystemConfigRepository systemConfigRepository,
    IAiRequestLogRepository aiRequestLogRepository,
    ITeamBuilderContextBuilder contextBuilder,
    ITeamBuilderResponseParser responseParser,
    ITeamBuilderResponseNormalizer teamBuilderResponseNormalizer,
    ITeamBuilderPromptBuilder promptBuilder,
    ILogger<TeamBuilderService> logger) : ITeamBuilderService
{
    public async Task<TeamBuilderResponseDto> BuildTeamAsync(
        long managerUserId,
        string? requirement,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "AI team builder requested by manager {ManagerUserId} with requirement: {Requirement}",
            managerUserId, requirement);

        ValidateRequirement(requirement);

        var rawContext = await contextBuilder.BuildTeamBuilderRawContextAsync(requirement!, cancellationToken);
        ApplyAssignablePool(rawContext);

        var jsonContext = contextBuilder.SerializeTeamBuilderContext(rawContext);
        var prompt = promptBuilder.BuildTeamBuilderPrompt(requirement!, jsonContext);

        var responseText = await GenerateCompletionAsync(prompt, cancellationToken);
        TeamBuilderResponseDto parsed;
        try
        {
            parsed = responseParser.ParseTeamBuilder(responseText);
        }
        catch (ValidationAppException ex) when (ex.ErrorCode == ErrorCodes.LlmResponseInvalid)
        {
            logger.LogWarning("Team builder initial parse failed for manager {ManagerUserId}, retrying with repair prompt", managerUserId);
            var repairPrompt = promptBuilder.BuildTeamBuilderRepairPrompt(requirement!, responseText);
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
