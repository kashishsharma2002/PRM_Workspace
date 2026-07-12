using Server.Models.DTOs.Ai;

namespace Server.Services.TeamBuilder.Abstractions;

public interface ITeamBuilderResponseParser
{
    TeamBuilderResponseDto ParseTeamBuilder(string responseText);
}
