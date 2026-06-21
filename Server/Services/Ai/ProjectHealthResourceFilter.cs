using Server.Models.DTOs.Ai.Context;
using Server.Services.Ai.Abstractions;

namespace Server.Services.Ai;

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
