using Server.Repositories.SystemConfig;

namespace Server.AI.Abstractions;

public interface ILlmConfigResolver
{
    Task<string> ResolveModelAsync(
        ISystemConfigRepository systemConfigRepository,
        string configKey,
        string defaultModel,
        CancellationToken cancellationToken = default);
}
