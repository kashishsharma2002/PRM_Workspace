using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.AI.Infrastructure;
using Server.Common;
using Server.Repositories.SystemConfig;

namespace Server.AI.Providers;

public class GeminiClient(
    IHttpClientFactory httpClientFactory,
    ISystemConfigRepository systemConfigRepository,
    ILlmApiKeyResolver llmApiKeyResolver,
    ILlmConfigResolver llmConfigResolver,
    IOptions<LlmSettings> llmSettings,
    ILogger<GeminiClient> logger)
    : LlmClientBase(systemConfigRepository, llmApiKeyResolver, logger)
{
    public override string ProviderKey => LlmProviderKeys.Gemini;
    public override string ApiConfigKey => ConfigKeys.LlmApiKeyGemini;

    public override async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandlingAsync("Gemini", async () =>
        {
            var apiKey = await ResolveApiKeyAsync(cancellationToken);

            var geminiSettings = llmSettings.Value.Gemini;
            var model = await llmConfigResolver.ResolveModelAsync(
                SystemConfigRepository,
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
        });
    }
}
