using Server.Common;

namespace Server.AI;

public class GeminiClient : ILlmClient
{
    public string ProviderKey => LlmProviders.Gemini;

    public Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Gemini integration is available in Phase 8.");
}
