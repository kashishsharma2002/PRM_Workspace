// Server/AI = infrastructure adapters (HTTP to external LLM APIs).
// Application orchestration lives in Server/Services/Ai.
// Data access lives in Server/Repositories/Ai.

namespace Server.AI.Abstractions;

public interface ILlmClient
{
    string ProviderKey { get; }
    string ApiConfigKey { get; }

    Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default);
}
