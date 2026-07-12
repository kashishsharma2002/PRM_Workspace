using Server.Models.DTOs.SkillMatching.Context;

namespace Server.Services.SkillMatching.Abstractions;

public interface ISkillMatchContextAssembler
{
    Task<AiSkillMatchRawData> LoadRawDataAsync(CancellationToken cancellationToken = default);

    List<AiSkillMatchCandidateContext> AssembleCandidates(AiSkillMatchRawData rawData);

    Task<List<AiSkillMatchCandidateContext>> AssembleCandidatesAsync(CancellationToken cancellationToken = default);
}
