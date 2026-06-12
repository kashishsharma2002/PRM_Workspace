namespace Server.AI.Configuration;

public class LlmSettings
{
    public GeminiLlmSettings Gemini { get; set; } = new();
    public GroqLlmSettings Groq { get; set; } = new();
    public GemmaLlmSettings Gemma { get; set; } = new();
    public int HttpTimeoutSeconds { get; set; } = 120;
}

public class GeminiLlmSettings
{
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    public string ApiVersion { get; set; } = "v1beta";
    public string DefaultModel { get; set; } = "gemini-2.0-flash";
}

public class GroqLlmSettings
{
    public string BaseUrl { get; set; } = "https://api.groq.com";
    public string ChatCompletionsPath { get; set; } = "/openai/v1/chat/completions";
    public string DefaultModel { get; set; } = "llama-3.3-70b-versatile";
}

public class GemmaLlmSettings
{
    public string GenerateUrl { get; set; } = "http://localhost:11434/api/generate";
    public string DefaultModel { get; set; } = "gemma3:12b-it-q8_0";
}
