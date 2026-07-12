using System.Net.Http.Headers;
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

public class GroqClient(
    IHttpClientFactory httpClientFactory,
    ISystemConfigRepository systemConfigRepository,
    ILlmApiKeyResolver llmApiKeyResolver,
    ILlmConfigResolver llmConfigResolver,
    IOptions<LlmSettings> llmSettings,
    ILogger<GroqClient> logger)
    : LlmClientBase(systemConfigRepository, llmApiKeyResolver, logger)
{
    public override string ProviderKey => LlmProviderKeys.Groq;
    public override string ApiConfigKey => ConfigKeys.LlmApiKeyGroq;

    public override async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandlingAsync("Groq", async () =>
        {
            var apiKey = await ResolveApiKeyAsync(cancellationToken);

            var groqSettings = llmSettings.Value.Groq;
            var model = await llmConfigResolver.ResolveModelAsync(
                SystemConfigRepository,
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
        });
    }
}
