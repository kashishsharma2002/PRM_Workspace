using Server.Common;
using Server.Repositories.SystemConfig;

namespace Server.AI;

public static class LlmConfigResolver
{
    public static async Task<string> ResolveModelAsync(
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
