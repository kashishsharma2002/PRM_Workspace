namespace Server.AI;

public interface ILlmClient
{
    string ProviderKey { get; }

    Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default);
}
