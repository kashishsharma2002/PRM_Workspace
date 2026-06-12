namespace Server.AI;

public class LlmClientFactory(IEnumerable<ILlmClient> clients) : ILlmClientFactory
{
    private readonly Dictionary<string, ILlmClient> _clients = clients
        .ToDictionary(c => c.ProviderKey, c => c, StringComparer.OrdinalIgnoreCase);

    public ILlmClient CreateClient(string providerKey)
    {
        if (!_clients.TryGetValue(providerKey, out var client))
        {
            throw new InvalidOperationException($"Unsupported LLM provider: {providerKey}");
        }
        return client;
    }
}
