using System.Text.Json;
using Microsoft.Extensions.Logging;
using Server.Common.Errors;
using Server.Common.Llm;
using Server.Exceptions;
using Server.Models.DTOs.SkillMatching;
using Server.Services.SkillMatching.Abstractions;

namespace Server.Services.SkillMatching;

public class SkillMatchResponseParser(ILogger<SkillMatchResponseParser> logger) : ISkillMatchResponseParser
{
    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

    public AiSkillMatchResponseDto ParseSkillMatch(string responseText, long projectId)
    {
        try
        {
            var cleanText = LlmResponseJsonHelper.ExtractJson(responseText);
            var parsed = JsonSerializer.Deserialize<AiSkillMatchResponseDto>(cleanText, DeserializeOptions);
            if (parsed is not null)
                return parsed;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse AI skill match response for project {ProjectId}", projectId);
            throw new ValidationAppException(
                "The AI returned a response that could not be parsed. Try again or switch LLM provider.",
                errorCode: ErrorCodes.LlmResponseInvalid);
        }

        throw new ValidationAppException(
            "The AI returned an empty or invalid skill match result. Try again or switch LLM provider.",
            errorCode: ErrorCodes.LlmResponseInvalid);
    }
}
