using Server.Services.Emails.Templates;
using Xunit;

namespace Tests.Services.Emails;

public class TemplateRenderingServiceTests
{
    private readonly TemplateRenderingService _service = new();

    [Fact]
    public void Render_ReplacesPlaceholders()
    {
        var template = "Hello {{EmployeeName}}, week ending {{WeekEndDate}}.";
        var result = _service.Render(template, new Dictionary<string, string>
        {
            ["EmployeeName"] = "Ravi Kumar",
            ["WeekEndDate"] = "2026-06-08"
        });

        Assert.Equal("Hello Ravi Kumar, week ending 2026-06-08.", result);
    }

    [Fact]
    public void Render_ReturnsEmpty_ForNullTemplate()
    {
        var result = _service.Render(string.Empty, new Dictionary<string, string> { ["A"] = "B" });
        Assert.Equal(string.Empty, result);
    }
}
