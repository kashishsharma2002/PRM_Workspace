using Server.Models.DTOs.Ai.Context;

namespace Server.Services.Ai.Abstractions;

public interface IAiContextBuilder
{
    Task<(AiRiskContextModel Context, string Json)> BuildRiskContextAsync(
        long projectId,
        CancellationToken cancellationToken = default);

    Task<(AiSkillMatchContextModel Context, string Json)> BuildSkillMatchContextAsync(
        long projectId,
        CancellationToken cancellationToken = default);

    Task<(AiOrganizationalSkillMatchContextModel Context, string Json)> BuildOrganizationalSkillMatchContextAsync(
        CancellationToken cancellationToken = default);

    Task<AiTeamBuilderContextModel> BuildTeamBuilderRawContextAsync(
        string requirement,
        CancellationToken cancellationToken = default);

    string SerializeTeamBuilderContext(AiTeamBuilderContextModel context);
}
