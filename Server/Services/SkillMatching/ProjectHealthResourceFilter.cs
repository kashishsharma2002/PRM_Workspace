using Server.Models.DTOs.SkillMatching.Context;
using Server.Services.SkillMatching.Abstractions;

namespace Server.Services.SkillMatching;

public class ProjectHealthResourceFilter : IProjectHealthResourceFilter
{
    public List<AiSkillMatchCandidateContext> FilterForAtRiskEmail(
        List<AiSkillMatchCandidateContext> candidates,
        long excludeProjectId) =>
        candidates
            .Where(c => c.RemainingCapacityPercentage > 0)
            .Where(c => !c.ActiveAllocations.Any(a => a.ProjectId == excludeProjectId))
            .OrderByDescending(c => c.RemainingCapacityPercentage)
            .ToList();
}
