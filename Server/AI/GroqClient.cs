using Server.Common;

namespace Server.AI;

public class GroqClient : ILlmClient
{
    public string ProviderKey => LlmProviders.Groq;

    public Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Groq integration is available in Phase 8.");
}
