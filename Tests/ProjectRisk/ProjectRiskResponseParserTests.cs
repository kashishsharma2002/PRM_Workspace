using Server.Common.Llm;
using Server.Exceptions;
using Server.Models.DTOs.ProjectRisk;
using Server.Services.ProjectRisk;
using Tests.Helpers;

namespace Tests.ProjectRisk;

public class ProjectRiskResponseParserTests
{
    private readonly ProjectRiskResponseParser _parser =
        new(TestServiceFactory.CreateLogger<ProjectRiskResponseParser>());

    [Fact]
    public void ExtractJson_StripsMarkdownWrapper()
    {
        var input = "Here is the result:\n```json\n{\"projectId\": 5, \"summary\": \"ok\"}\n```";

        var json = LlmResponseJsonHelper.ExtractJson(input);

        Assert.Contains("\"projectId\": 5", json);
        Assert.DoesNotContain("```", json);
    }

    [Fact]
    public void ParseRiskSummary_ReturnsParsedDto_WhenValidJson()
    {
        var response = """
            {
              "projectId": 10,
              "summary": "Timeline risk detected.",
              "recommendations": ["Review milestones"]
            }
            """;

        var result = _parser.ParseRiskSummary(response, 10);

        Assert.Equal(10, result.ProjectId);
        Assert.Equal("Timeline risk detected.", result.Summary);
        Assert.Single(result.Recommendations);
    }
}
