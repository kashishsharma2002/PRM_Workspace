namespace Server.AI;

public class GeminiClient : ILlmClient
{
    public Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Gemini integration is available in Phase 8.");
}
