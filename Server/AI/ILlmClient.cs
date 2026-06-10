namespace Server.AI;

public interface ILlmClient
{
    Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default);
}
