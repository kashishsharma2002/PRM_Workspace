using Server.Models.DTOs.Ai.Context;

namespace Server.Services.Ai;

public class ProjectHealthResourceFilter
{
    public List<AiSkillMatchCandidateContext> FilterForAtRiskEmail(
        IReadOnlyList<AiSkillMatchCandidateContext> candidates,
        long projectId)
    {
        return candidates
            .Where(c => c.RemainingCapacityPercentage > 0)
            .Where(c => !c.ActiveAllocations.Any(a => a.ProjectId == projectId))
            .OrderByDescending(c => c.RemainingCapacityPercentage)
            .ToList();
    }
}
