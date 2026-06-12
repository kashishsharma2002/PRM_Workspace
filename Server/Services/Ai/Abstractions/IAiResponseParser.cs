using Server.Models.DTOs.Ai;

namespace Server.Services.Ai.Abstractions;

public interface IAiResponseParser
{
    AiRiskSummaryResponseDto ParseRiskSummary(string responseText, long projectId);
    AiSkillMatchResponseDto ParseSkillMatch(string responseText, long projectId);
    TeamBuilderResponseDto ParseTeamBuilder(string responseText);
}
