using Server.Models.DTOs.ProjectRisk;

namespace Server.Services.ProjectRisk.Abstractions;

public interface IProjectRiskResponseParser
{
    AiRiskSummaryResponseDto ParseRiskSummary(string responseText, long projectId);
}
