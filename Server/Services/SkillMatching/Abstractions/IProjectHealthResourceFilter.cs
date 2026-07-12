using Server.Models.DTOs.SkillMatching.Context;

namespace Server.Services.SkillMatching.Abstractions;

public interface IProjectHealthResourceFilter
{
    List<AiSkillMatchCandidateContext> FilterForAtRiskEmail(
        List<AiSkillMatchCandidateContext> candidates,
        long excludeProjectId);
}
