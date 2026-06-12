using Server.Repositories.SystemConfig;

namespace Server.AI.Abstractions;

public interface ILlmApiKeyResolver
{
    Task<string> ResolveAsync(
        ISystemConfigRepository systemConfigRepository,
        string providerKey,
        CancellationToken cancellationToken = default);
}
