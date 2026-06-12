using Server.AI.Configuration;

namespace Server.Common;

[Obsolete("Use Server.AI.Configuration.LlmProviderKeys instead.")]
public static class LlmProviders
{
    public const string Gemini = LlmProviderKeys.Gemini;
    public const string Groq = LlmProviderKeys.Groq;
    public const string Gemma = LlmProviderKeys.Gemma;
}
