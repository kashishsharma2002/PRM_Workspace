namespace Server.AI;

public class LlmClientResolver(IEnumerable<ILlmClient> clients) : ILlmClientResolver
{
    private readonly IReadOnlyDictionary<string, ILlmClient> _clientsByProvider = clients
        .ToDictionary(client => client.ProviderKey, StringComparer.OrdinalIgnoreCase);

    public ILlmClient Resolve(string provider)
    {
        if (!_clientsByProvider.TryGetValue(provider, out var client))
            throw new InvalidOperationException($"Unsupported LLM provider: {provider}");

        return client;
    }
}
