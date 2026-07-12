using System.Text.Json;
using Server.Models.DTOs.Ai.Context;
using Server.Services.TeamBuilder.Abstractions;
using Server.Services.SkillMatching.Abstractions;

namespace Server.Services.TeamBuilder;

public class TeamBuilderContextBuilder(ISkillMatchContextAssembler skillMatchContextAssembler) : ITeamBuilderContextBuilder
{
    private static readonly JsonSerializerOptions TeamBuilderJsonOptions = new() { WriteIndented = false };

    public async Task<AiTeamBuilderContextModel> BuildTeamBuilderRawContextAsync(
        string requirement,
        CancellationToken cancellationToken = default)
    {
        var allCandidates = await skillMatchContextAssembler.AssembleCandidatesAsync(cancellationToken);
        return new AiTeamBuilderContextModel
        {
            ManagerRequirement = requirement,
            AllCandidates = allCandidates
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

    private static AiTeamBuilderCandidatePromptContext ToPromptCandidate(
        Server.Models.DTOs.SkillMatching.Context.AiSkillMatchCandidateContext candidate) =>
        new()
        {
            FullName = candidate.FullName,
            Designation = candidate.Designation,
            Skills = candidate.Skills,
            RemainingCapacityPercentage = candidate.RemainingCapacityPercentage,
            ActiveAllocations = candidate.ActiveAllocations
        };
}
