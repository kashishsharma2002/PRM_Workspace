using Server.AI;
using Server.Common;

namespace Tests;

public class LlmClientResolverTests
{
    [Fact]
    public void Resolve_ReturnsRegisteredClient_ForKnownProvider()
    {
        var resolver = new LlmClientResolver([new GeminiClient(), new GroqClient()]);

        var client = resolver.Resolve(LlmProviders.Gemini);

        Assert.IsType<GeminiClient>(client);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        var resolver = new LlmClientResolver([new GeminiClient(), new GroqClient()]);

        var client = resolver.Resolve("groq");

        Assert.IsType<GroqClient>(client);
    }

    [Fact]
    public void Resolve_Throws_ForUnknownProvider()
    {
        var resolver = new LlmClientResolver([new GeminiClient(), new GroqClient()]);

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("OPENAI"));

        Assert.Contains("OPENAI", exception.Message);
    }
}
