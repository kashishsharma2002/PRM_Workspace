namespace Server.AI;

public class LlmClientFactory(IServiceProvider serviceProvider)
{
    public ILlmClient Create(string provider) => provider.ToUpperInvariant() switch
    {
        "GEMINI" => serviceProvider.GetRequiredService<GeminiClient>(),
        "GROQ" => serviceProvider.GetRequiredService<GroqClient>(),
        _ => throw new InvalidOperationException($"Unsupported LLM provider: {provider}")
    };
}
