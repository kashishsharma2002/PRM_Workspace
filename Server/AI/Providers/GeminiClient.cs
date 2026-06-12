using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.AI.Infrastructure;
using Server.Common;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Repositories.SystemConfig;

namespace Server.AI.Providers;

public class GeminiClient(
    IHttpClientFactory httpClientFactory,
    ISystemConfigRepository systemConfigRepository,
    ILlmApiKeyResolver llmApiKeyResolver,
    ILlmConfigResolver llmConfigResolver,
    IOptions<LlmSettings> llmSettings,
    ILogger<GeminiClient> logger) : ILlmClient
{
    public string ProviderKey => LlmProviderKeys.Gemini;

    public async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = await llmApiKeyResolver.ResolveAsync(systemConfigRepository, LlmProviderKeys.Gemini, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("***"))
            throw new AiServiceAppException(
                "LLM API key is not configured. Set the API key in Admin → System Configuration.",
                ErrorCodes.LlmNotConfigured);

        logger.LogDebug("Gemini request using API key prefix {KeyPrefix}", LlmHttpErrorHelper.MaskKeyPrefix(apiKey));

        var geminiSettings = llmSettings.Value.Gemini;
        var model = await llmConfigResolver.ResolveModelAsync(
            systemConfigRepository,
            ConfigKeys.LlmModelGemini,
            geminiSettings.DefaultModel,
            cancellationToken);

        var client = httpClientFactory.CreateClient("GeminiClient");
        var baseUrl = geminiSettings.BaseUrl.TrimEnd('/');
        var url =
            $"{baseUrl}/{geminiSettings.ApiVersion}/models/{model}:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        try
        {
            var response = await client.PostAsJsonAsync(url, requestBody, cancellationToken);
            await LlmHttpErrorHelper.ThrowIfNotSuccessAsync(response, "Gemini", logger, cancellationToken);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (json.TryGetProperty("candidates", out var candidates) &&
                candidates.ValueKind == JsonValueKind.Array &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.ValueKind == JsonValueKind.Array &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var text))
            {
                return text.GetString() ?? string.Empty;
            }

            throw new InvalidOperationException("Invalid response format from Gemini API.");
        }
        catch (Exception ex) when (ex is not AiServiceAppException)
        {
            logger.LogError(ex, "Gemini API call failed.");
            throw new AiServiceAppException(
                "Gemini AI request failed unexpectedly. Check server logs for details.",
                ErrorCodes.LlmRequestFailed);
        }
    }
}
