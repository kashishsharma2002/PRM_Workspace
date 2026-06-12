using System.Net.Http.Headers;
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

public class GroqClient(
    IHttpClientFactory httpClientFactory,
    ISystemConfigRepository systemConfigRepository,
    ILlmApiKeyResolver llmApiKeyResolver,
    ILlmConfigResolver llmConfigResolver,
    IOptions<LlmSettings> llmSettings,
    ILogger<GroqClient> logger) : ILlmClient
{
    public string ProviderKey => LlmProviderKeys.Groq;

    public async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = await llmApiKeyResolver.ResolveAsync(systemConfigRepository, LlmProviderKeys.Groq, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("***"))
            throw new AiServiceAppException(
                "LLM API key is not configured. Set the API key in Admin → System Configuration.",
                ErrorCodes.LlmNotConfigured);

        logger.LogDebug("Groq request using API key prefix {KeyPrefix}", LlmHttpErrorHelper.MaskKeyPrefix(apiKey));

        var groqSettings = llmSettings.Value.Groq;
        var model = await llmConfigResolver.ResolveModelAsync(
            systemConfigRepository,
            ConfigKeys.LlmModelGroq,
            groqSettings.DefaultModel,
            cancellationToken);

        var client = httpClientFactory.CreateClient("GroqClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var url = groqSettings.ChatCompletionsPath;
        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        try
        {
            var response = await client.PostAsJsonAsync(url, requestBody, cancellationToken);
            await LlmHttpErrorHelper.ThrowIfNotSuccessAsync(response, "Groq", logger, cancellationToken);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (json.TryGetProperty("choices", out var choices) &&
                choices.ValueKind == JsonValueKind.Array &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }

            throw new InvalidOperationException("Invalid response format from Groq API.");
        }
        catch (Exception ex) when (ex is not AiServiceAppException)
        {
            logger.LogError(ex, "Groq API call failed.");
            throw new AiServiceAppException(
                "Groq AI request failed unexpectedly. Check server logs for details.",
                ErrorCodes.LlmRequestFailed);
        }
    }
}
