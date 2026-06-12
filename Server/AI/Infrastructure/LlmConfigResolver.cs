using Server.AI.Abstractions;
using Server.Repositories.SystemConfig;

namespace Server.AI.Infrastructure;

public class LlmConfigResolver : ILlmConfigResolver
{
    public async Task<string> ResolveModelAsync(
        ISystemConfigRepository systemConfigRepository,
        string configKey,
        string defaultModel,
        CancellationToken cancellationToken = default)
    {
        var config = await systemConfigRepository.GetByKeyAsync(configKey, cancellationToken);
        return string.IsNullOrWhiteSpace(config?.ConfigValue)
            ? defaultModel
            : config.ConfigValue.Trim();
    }
}
