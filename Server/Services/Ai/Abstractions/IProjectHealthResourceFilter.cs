using Server.Models.DTOs.Ai.Context;

namespace Server.Services.Ai.Abstractions;

public interface IProjectHealthResourceFilter
{
    List<AiSkillMatchCandidateContext> FilterForAtRiskEmail(
        List<AiSkillMatchCandidateContext> candidates,
        long excludeProjectId);
}
