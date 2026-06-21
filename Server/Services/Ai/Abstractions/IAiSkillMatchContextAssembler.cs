using Server.Models.DTOs.Ai.Context;

namespace Server.Services.Ai.Abstractions;

public interface IAiSkillMatchContextAssembler
{
    List<AiSkillMatchCandidateContext> AssembleCandidates(AiSkillMatchRawData rawData);
}
