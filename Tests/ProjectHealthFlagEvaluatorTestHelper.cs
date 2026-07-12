using Server.Services.Projects;
using Server.Services.Projects.Abstractions;
using Server.Services.Projects.Rules;

namespace Tests;

public static class ProjectHealthFlagEvaluatorTestHelper
{
    public static IProjectHealthFlagEvaluator CreateEvaluator() =>
        new ProjectHealthFlagEvaluator(
        [
            new OverdueMilestoneFlagRule(),
            new LowHoursFlagRule(),
            new ApproachingDeadlineFlagRule()
        ]);
}
