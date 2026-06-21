using System.Text.Json;
using Server.Models.DTOs.Ai.Context;
using Server.Repositories.Ai;
using Server.Services.Ai.Abstractions;

namespace Server.Services.Ai;

public class AiContextBuilder(
    IAiContextRepository aiContextRepository,
    IAiSkillMatchContextAssembler skillMatchContextAssembler) : IAiContextBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly JsonSerializerOptions TeamBuilderJsonOptions = new() { WriteIndented = false };

    public async Task<(AiRiskContextModel Context, string Json)> BuildRiskContextAsync(
        long projectId,
        CancellationToken cancellationToken = default)
    {
        var context = await aiContextRepository.GetRiskContextAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project with ID {projectId} not found.");

        return (context, JsonSerializer.Serialize(context, JsonOptions));
    }

    public async Task<(AiSkillMatchContextModel Context, string Json)> BuildSkillMatchContextAsync(
        long projectId,
        CancellationToken cancellationToken = default)
    {
        var projectContext = await aiContextRepository.GetSkillMatchContextAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project with ID {projectId} not found.");

        var rawData = await aiContextRepository.GetSkillMatchRawDataAsync(cancellationToken);
        projectContext.Candidates = skillMatchContextAssembler.AssembleCandidates(rawData);

        return (projectContext, JsonSerializer.Serialize(projectContext, JsonOptions));
    }

    public async Task<(AiOrganizationalSkillMatchContextModel Context, string Json)> BuildOrganizationalSkillMatchContextAsync(
        CancellationToken cancellationToken = default)
    {
        var rawData = await aiContextRepository.GetSkillMatchRawDataAsync(cancellationToken);
        var context = new AiOrganizationalSkillMatchContextModel
        {
            Candidates = skillMatchContextAssembler.AssembleCandidates(rawData)
        };

        return (context, JsonSerializer.Serialize(context, JsonOptions));
    }

    public async Task<AiTeamBuilderContextModel> BuildTeamBuilderRawContextAsync(
        string requirement,
        CancellationToken cancellationToken = default)
    {
        var rawData = await aiContextRepository.GetSkillMatchRawDataAsync(cancellationToken);
        return new AiTeamBuilderContextModel
        {
            ManagerRequirement = requirement,
            AllCandidates = skillMatchContextAssembler.AssembleCandidates(rawData)
        };
    }

    public string SerializeTeamBuilderContext(AiTeamBuilderContextModel context)
    {
        var compact = new AiTeamBuilderPromptContextModel
        {
            ManagerRequirement = context.ManagerRequirement,
            AssignableCandidates = context.AssignableCandidates.Select(ToPromptCandidate).ToList(),
            AllCandidates = context.AllCandidates.Select(ToPromptCandidate).ToList()
        };

        return JsonSerializer.Serialize(compact, TeamBuilderJsonOptions);
    }

    private static AiTeamBuilderCandidatePromptContext ToPromptCandidate(AiSkillMatchCandidateContext candidate) =>
        new()
        {
            FullName = candidate.FullName,
            Designation = candidate.Designation,
            Skills = candidate.Skills,
            RemainingCapacityPercentage = candidate.RemainingCapacityPercentage,
            ActiveAllocations = candidate.ActiveAllocations
        };
}
