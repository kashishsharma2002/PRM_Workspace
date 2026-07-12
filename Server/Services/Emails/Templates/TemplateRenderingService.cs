using System.Text;

namespace Server.Services.Emails.Templates;

public class TemplateRenderingService : ITemplateRenderingService
{
    public string Render(string templateContent, Dictionary<string, string> placeholders)
    {
        if (string.IsNullOrEmpty(templateContent))
            return string.Empty;

        var builder = new StringBuilder(templateContent);
        foreach (var placeholder in placeholders)
            builder.Replace("{{" + placeholder.Key + "}}", placeholder.Value);

        return builder.ToString();
    }
}
