using System.Text.Json;
using Server.Repositories.Ai;
using Server.Services.Ai.Models;

namespace Server.Services.Ai;

public class AiContextBuilder(IAiContextRepository aiContextRepository)
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
        var context = await aiContextRepository.GetSkillMatchContextAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project with ID {projectId} not found.");

        return (context, JsonSerializer.Serialize(context, JsonOptions));
    }

    public async Task<(AiOrganizationalSkillMatchContextModel Context, string Json)> BuildOrganizationalSkillMatchContextAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await aiContextRepository.GetOrganizationalSkillMatchContextAsync(cancellationToken);
        return (context, JsonSerializer.Serialize(context, JsonOptions));
    }

    public async Task<AiTeamBuilderContextModel> BuildTeamBuilderRawContextAsync(
        string requirement,
        CancellationToken cancellationToken = default)
    {
        var orgContext = await aiContextRepository.GetOrganizationalSkillMatchContextAsync(cancellationToken);
        return new AiTeamBuilderContextModel
        {
            ManagerRequirement = requirement,
            AllCandidates = orgContext.Candidates
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
