using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.AI.Infrastructure;

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
            new StubLlmClient(LlmProviderKeys.Gemini),
            new StubLlmClient(LlmProviderKeys.Groq)
        ]);

        var client = factory.CreateClient(LlmProviderKeys.Gemini);

        Assert.Equal(LlmProviderKeys.Gemini, client.ProviderKey);
    }

    [Fact]
    public void CreateClient_IsCaseInsensitive()
    {
        var factory = new LlmClientFactory([
            new StubLlmClient(LlmProviderKeys.Gemini),
            new StubLlmClient(LlmProviderKeys.Groq)
        ]);

        var client = factory.CreateClient("groq");

        Assert.Equal(LlmProviderKeys.Groq, client.ProviderKey);
    }

    [Fact]
    public void CreateClient_Throws_ForUnknownProvider()
    {
        var factory = new LlmClientFactory([new StubLlmClient(LlmProviderKeys.Gemini)]);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient("OPENAI"));

        Assert.Contains("OPENAI", exception.Message);
    }
}
