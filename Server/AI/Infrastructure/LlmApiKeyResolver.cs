using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.Common;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Repositories.SystemConfig;

namespace Server.AI.Infrastructure;

public class LlmApiKeyResolver(IConfigEncryptionHelper encryptionHelper) : ILlmApiKeyResolver
{
    public async Task<string> ResolveAsync(
        ISystemConfigRepository systemConfigRepository,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        var configKey = GetConfigKeyForProvider(providerKey);
        var config = await systemConfigRepository.GetByKeyAsync(configKey, cancellationToken);

        if ((config is null || string.IsNullOrWhiteSpace(config.ConfigValue)) && configKey != ConfigKeys.LlmApiKey)
            config = await systemConfigRepository.GetByKeyAsync(ConfigKeys.LlmApiKey, cancellationToken);

        if (config is null || string.IsNullOrWhiteSpace(config.ConfigValue))
            return string.Empty;

        var stored = config.ConfigValue;
        if (!encryptionHelper.IsEncrypted(stored))
            return stored;

        try
        {
            return encryptionHelper.Decrypt(stored);
        }
        catch
        {
            throw new AiServiceAppException(
                "API key could not be decrypted — re-enter key in System Configuration.",
                ErrorCodes.LlmNotConfigured);
        }
    }

    private static string GetConfigKeyForProvider(string providerKey) =>
        providerKey.ToUpperInvariant() switch
        {
            LlmProviderKeys.Gemini => ConfigKeys.LlmApiKeyGemini,
            LlmProviderKeys.Groq => ConfigKeys.LlmApiKeyGroq,
            LlmProviderKeys.Gemma => ConfigKeys.LlmApiKey,
            _ => ConfigKeys.LlmApiKey
        };
}
