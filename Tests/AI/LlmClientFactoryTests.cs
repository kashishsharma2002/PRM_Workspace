using Server.AI;
using Server.Common;

namespace Tests;

public class LlmClientFactoryTests
{
    private sealed class StubLlmClient(string providerKey) : ILlmClient
    {
        public string ProviderKey { get; } = providerKey;
        public Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default) =>
            Task.FromResult("{}");
    }

    [Fact]
    public void CreateClient_ReturnsRegisteredClient_ForKnownProvider()
    {
        var factory = new LlmClientFactory([
            new StubLlmClient(LlmProviders.Gemini),
            new StubLlmClient(LlmProviders.Groq)
        ]);

        var client = factory.CreateClient(LlmProviders.Gemini);

        Assert.Equal(LlmProviders.Gemini, client.ProviderKey);
    }

    [Fact]
    public void CreateClient_IsCaseInsensitive()
    {
        var factory = new LlmClientFactory([
            new StubLlmClient(LlmProviders.Gemini),
            new StubLlmClient(LlmProviders.Groq)
        ]);

        var client = factory.CreateClient("groq");

        Assert.Equal(LlmProviders.Groq, client.ProviderKey);
    }

    [Fact]
    public void CreateClient_Throws_ForUnknownProvider()
    {
        var factory = new LlmClientFactory([new StubLlmClient(LlmProviders.Gemini)]);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient("OPENAI"));

        Assert.Contains("OPENAI", exception.Message);
    }
}
