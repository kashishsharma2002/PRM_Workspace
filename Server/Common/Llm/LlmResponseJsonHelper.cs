namespace Server.Common.Llm;

public static class LlmResponseJsonHelper
{
    public static string ExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text.Substring(start, end - start + 1);

        return text;
    }
}
