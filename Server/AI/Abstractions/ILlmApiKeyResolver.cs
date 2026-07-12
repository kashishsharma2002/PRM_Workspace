using Server.Repositories.SystemConfig;

namespace Server.AI.Abstractions;

public interface ILlmApiKeyResolver
{
    Task<string> ResolveAsync(
        ISystemConfigRepository systemConfigRepository,
        string configKey,
        CancellationToken cancellationToken = default);
}
