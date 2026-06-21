using Server.AI.Abstractions;
using Server.Common;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Repositories.SystemConfig;

namespace Server.AI.Infrastructure;

public class LlmApiKeyResolver(IConfigEncryptionHelper encryptionHelper) : ILlmApiKeyResolver
{
    public async Task<string> ResolveAsync(
        ISystemConfigRepository systemConfigRepository,
        string configKey,
        CancellationToken cancellationToken = default)
    {
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
}
