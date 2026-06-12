using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Repositories.SystemConfig;

namespace Server.AI;

public class GeminiClient(
    IHttpClientFactory httpClientFactory,
    ISystemConfigRepository systemConfigRepository,
    ConfigEncryptionHelper encryptionHelper,
    ILogger<GeminiClient> logger) : ILlmClient
{
    public string ProviderKey => LlmProviders.Gemini;

    public async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = await LlmApiKeyResolver.ResolveAsync(systemConfigRepository, encryptionHelper, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("***"))
            throw new AiServiceAppException(
                "LLM API key is not configured. Set the API key in Admin → System Configuration.",
                ErrorCodes.LlmNotConfigured);

        logger.LogDebug("Gemini request using API key prefix {KeyPrefix}", MaskKeyPrefix(apiKey));

        var model = await LlmConfigResolver.ResolveModelAsync(
            systemConfigRepository,
            ConfigKeys.LlmModelGemini,
            LlmDefaults.GeminiModel,
            cancellationToken);

        var client = httpClientFactory.CreateClient("GeminiClient");
        var url =
            $"https://generativelanguage.googleapis.com/{LlmDefaults.GeminiApiVersion}/models/{model}:generateContent?key={apiKey}";

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

    private static string MaskKeyPrefix(string apiKey) =>
        apiKey.Length <= 4 ? "****" : apiKey[..4] + "...";
}
