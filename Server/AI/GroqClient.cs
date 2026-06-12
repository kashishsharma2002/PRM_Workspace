using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Repositories.SystemConfig;

namespace Server.AI;

public class GroqClient(
    IHttpClientFactory httpClientFactory,
    ISystemConfigRepository systemConfigRepository,
    ConfigEncryptionHelper encryptionHelper,
    ILogger<GroqClient> logger) : ILlmClient
{
    public string ProviderKey => LlmProviders.Groq;

    public async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = await LlmApiKeyResolver.ResolveAsync(systemConfigRepository, encryptionHelper, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("***"))
            throw new AiServiceAppException(
                "LLM API key is not configured. Set the API key in Admin → System Configuration.",
                ErrorCodes.LlmNotConfigured);

        logger.LogDebug("Groq request using API key prefix {KeyPrefix}", MaskKeyPrefix(apiKey));

        var model = await LlmConfigResolver.ResolveModelAsync(
            systemConfigRepository,
            ConfigKeys.LlmModelGroq,
            LlmDefaults.GroqModel,
            cancellationToken);

        var client = httpClientFactory.CreateClient("GroqClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var url = "https://api.groq.com/openai/v1/chat/completions";
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

    private static string MaskKeyPrefix(string apiKey) =>
        apiKey.Length <= 4 ? "****" : apiKey[..4] + "...";
}
