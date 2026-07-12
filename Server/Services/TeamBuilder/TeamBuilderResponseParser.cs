using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Server.Common.Errors;
using Server.Common.Llm;
using Server.Exceptions;
using Server.Models.DTOs.Ai;
using Server.Services.TeamBuilder.Abstractions;

namespace Server.Services.TeamBuilder;

public class TeamBuilderResponseParser(ILogger<TeamBuilderResponseParser> logger) : ITeamBuilderResponseParser
{
    private static readonly JsonSerializerOptions TeamBuilderDeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private const int TeamBuilderResponseLogSnippetLength = 500;

    public TeamBuilderResponseDto ParseTeamBuilder(string responseText)
    {
        try
        {
            var cleanText = LlmResponseJsonHelper.ExtractJson(responseText);
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
}
