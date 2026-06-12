using System.Net.Http.Json;
using System.Text;
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

public class GemmaClient(
    IHttpClientFactory httpClientFactory,
    ISystemConfigRepository systemConfigRepository,
    ILlmApiKeyResolver llmApiKeyResolver,
    ILlmConfigResolver llmConfigResolver,
    IOptions<LlmSettings> llmSettings,
    ILogger<GemmaClient> logger) : ILlmClient
{
    public string ProviderKey => LlmProviderKeys.Gemma;

    public async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = await llmApiKeyResolver.ResolveAsync(systemConfigRepository, LlmProviderKeys.Gemma, cancellationToken);
        var gemmaSettings = llmSettings.Value.Gemma;
        var model = await llmConfigResolver.ResolveModelAsync(
            systemConfigRepository,
            ConfigKeys.LlmModelGemma,
            gemmaSettings.DefaultModel,
            cancellationToken);

        var generateUrl = await ResolveGenerateUrlAsync(gemmaSettings.GenerateUrl, cancellationToken);

        var client = httpClientFactory.CreateClient("GemmaClient");
        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            prompt,
            stream = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, generateUrl)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("apikey", apiKey);

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            await LlmHttpErrorHelper.ThrowIfNotSuccessAsync(response, "Gemma", logger, cancellationToken);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (json.TryGetProperty("response", out var text))
                return text.GetString() ?? string.Empty;

            throw new InvalidOperationException("Invalid response format from Gemma Local API.");
        }
        catch (AiServiceAppException ex) when (ex.Message.Contains("No API key found in request", StringComparison.OrdinalIgnoreCase))
        {
            throw new AiServiceAppException(
                "Gemma/Ollama requires an apikey header. Leave API key empty in System Configuration or set your Ollama key.",
                ErrorCodes.LlmNotConfigured);
        }
        catch (Exception ex) when (ex is not AiServiceAppException)
        {
            logger.LogError(ex, "Local Gemma/Ollama API call failed.");
            throw new AiServiceAppException(
                "Local Gemma/Ollama service is unavailable. Ensure Ollama is running or switch to Gemini/Groq in System Configuration.",
                ErrorCodes.LlmRequestFailed);
        }
    }

    private async Task<string> ResolveGenerateUrlAsync(
        string defaultUrl,
        CancellationToken cancellationToken)
    {
        var config = await systemConfigRepository.GetByKeyAsync(ConfigKeys.LlmEndpointGemma, cancellationToken);
        return string.IsNullOrWhiteSpace(config?.ConfigValue)
            ? defaultUrl
            : config.ConfigValue.Trim();
    }
}
