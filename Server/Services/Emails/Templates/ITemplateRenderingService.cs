namespace Server.Services.Emails.Templates;

public interface ITemplateRenderingService
{
    string Render(string templateContent, Dictionary<string, string> placeholders);
}
