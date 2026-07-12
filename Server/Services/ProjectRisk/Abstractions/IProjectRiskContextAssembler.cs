using Server.Models.DTOs.ProjectRisk.Context;

namespace Server.Services.ProjectRisk.Abstractions;

public interface IProjectRiskContextAssembler
{
    Task<ProjectRiskContextModel> BuildAsync(long projectId, CancellationToken cancellationToken = default);
}
