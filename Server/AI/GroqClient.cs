namespace Server.AI;

public class GroqClient : ILlmClient
{
    public Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Groq integration is available in Phase 8.");
}
