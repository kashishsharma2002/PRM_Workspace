using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Models.DTOs.Ai;
using Server.Services.Ai.Abstractions;

namespace Server.Services.Ai;

public class AiResponseParser(ILogger<AiResponseParser> logger) : IAiResponseParser
{
    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly JsonSerializerOptions TeamBuilderDeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private const int TeamBuilderResponseLogSnippetLength = 500;

    public AiRiskSummaryResponseDto ParseRiskSummary(string responseText, long projectId)
    {
        try
        {
            var cleanText = ExtractJson(responseText);
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

    public AiSkillMatchResponseDto ParseSkillMatch(string responseText, long projectId)
    {
        try
        {
            var cleanText = ExtractJson(responseText);
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

    public TeamBuilderResponseDto ParseTeamBuilder(string responseText)
    {
        try
        {
            var cleanText = ExtractJson(responseText);
            var parsed = JsonSerializer.Deserialize<TeamBuilderResponseDto>(cleanText, TeamBuilderDeserializeOptions);
            if (parsed is not null)
            {
                parsed.Roles ??= [];
                return parsed;
            }
        }
        catch (Exception ex)
        {
            var snippet = responseText.Length > TeamBuilderResponseLogSnippetLength
                ? responseText[..TeamBuilderResponseLogSnippetLength]
                : responseText;
            logger.LogWarning(ex, "Failed to parse AI team builder response. Snippet: {ResponseSnippet}", snippet);
            throw new ValidationAppException(
                "The AI returned a response that could not be parsed. Try again or switch LLM provider.",
                errorCode: ErrorCodes.LlmResponseInvalid);
        }

        throw new ValidationAppException(
            "The AI returned an empty or invalid team builder result. Try again or switch LLM provider.",
            errorCode: ErrorCodes.LlmResponseInvalid);
    }

    public static string ExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text.Substring(start, end - start + 1);

        return text;
    }
}
