using Microsoft.Extensions.Logging;
using Server.AI.Abstractions;
using Server.AI.Infrastructure;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Repositories.SystemConfig;

namespace Server.AI.Providers;

public abstract class LlmClientBase : ILlmClient
{
    private readonly ISystemConfigRepository _systemConfigRepository;
    private readonly ILlmApiKeyResolver _llmApiKeyResolver;
    private readonly ILogger _logger;

    protected LlmClientBase(
        ISystemConfigRepository systemConfigRepository,
        ILlmApiKeyResolver llmApiKeyResolver,
        ILogger logger)
    {
        _systemConfigRepository = systemConfigRepository;
        _llmApiKeyResolver = llmApiKeyResolver;
        _logger = logger;
    }

    protected ISystemConfigRepository SystemConfigRepository => _systemConfigRepository;
    public abstract string ProviderKey { get; }
    public abstract string ApiConfigKey { get; }

    protected virtual bool RequiresConfiguredApiKey => true;

    public abstract Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default);

    protected async Task<string> ResolveApiKeyAsync(CancellationToken cancellationToken)
    {
        var apiKey = await _llmApiKeyResolver.ResolveAsync(
            _systemConfigRepository,
            ApiConfigKey,
            cancellationToken);

        if (RequiresConfiguredApiKey && (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("***")))
        {
            throw new AiServiceAppException(
                "LLM API key is not configured. Set the API key in Admin → System Configuration.",
                ErrorCodes.LlmNotConfigured);
        }

        _logger.LogDebug(
            "{Provider} request using API key prefix {KeyPrefix}",
            ProviderKey,
            LlmHttpErrorHelper.MaskKeyPrefix(apiKey));

        return apiKey;
    }

    protected async Task<string> ExecuteWithErrorHandlingAsync(
        string providerDisplayName,
        Func<Task<string>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is not AiServiceAppException)
        {
            _logger.LogError(ex, "{Provider} API call failed.", providerDisplayName);
            throw new AiServiceAppException(
                $"{providerDisplayName} AI request failed unexpectedly. Check server logs for details.",
                ErrorCodes.LlmRequestFailed);
        }
    }
}
