using System.Text.Json;
using Microsoft.Extensions.Logging;
using Server.Common.Errors;
using Server.Common.Llm;
using Server.Exceptions;
using Server.Models.DTOs.ProjectRisk;
using Server.Services.ProjectRisk.Abstractions;

namespace Server.Services.ProjectRisk;

public class ProjectRiskResponseParser(ILogger<ProjectRiskResponseParser> logger) : IProjectRiskResponseParser
{
    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

    public AiRiskSummaryResponseDto ParseRiskSummary(string responseText, long projectId)
    {
        try
        {
            var cleanText = LlmResponseJsonHelper.ExtractJson(responseText);
            var parsed = JsonSerializer.Deserialize<AiRiskSummaryResponseDto>(cleanText, DeserializeOptions);
            if (parsed is not null)
                return parsed;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse AI risk summary response for project {ProjectId}", projectId);
            throw new ValidationAppException(
                "The AI returned a response that could not be parsed. Try again or switch LLM provider.",
                errorCode: ErrorCodes.LlmResponseInvalid);
        }

        throw new ValidationAppException(
            "The AI returned an empty or invalid risk summary. Try again or switch LLM provider.",
            errorCode: ErrorCodes.LlmResponseInvalid);
    }
}
