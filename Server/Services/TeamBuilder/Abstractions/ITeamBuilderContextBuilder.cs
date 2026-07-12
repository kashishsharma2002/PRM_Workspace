using Server.Models.DTOs.Ai.Context;

namespace Server.Services.TeamBuilder.Abstractions;

public interface ITeamBuilderContextBuilder
{
    Task<AiTeamBuilderContextModel> BuildTeamBuilderRawContextAsync(
        string requirement,
        CancellationToken cancellationToken = default);

    string SerializeTeamBuilderContext(AiTeamBuilderContextModel context);
}
