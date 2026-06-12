using Server.Exceptions;
using Server.Models.DTOs.Ai;
using Server.Services.Ai;
using Tests.Helpers;

namespace Tests;

public class AiResponseParserTests
{
    private readonly AiResponseParser _parser = new(TestServiceFactory.CreateLogger<AiResponseParser>());

    [Fact]
    public void ExtractJson_StripsMarkdownWrapper()
    {
        var input = "Here is the result:\n```json\n{\"projectId\": 5, \"summary\": \"ok\"}\n```";

        var json = AiResponseParser.ExtractJson(input);

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

    [Fact]
    public void ParseSkillMatch_Throws_WhenInvalidJson()
    {
        Assert.Throws<ValidationAppException>(() => _parser.ParseSkillMatch("not valid json", 3));
    }

    [Fact]
    public void ParseSkillMatch_ParsesReasonField()
    {
        var response = """
            {
              "projectId": 1,
              "matches": [
                {
                  "employeeName": "Jane Doe",
                  "skillName": "React",
                  "matchScore": 92,
                  "reason": "Expert-level React skills with 60% capacity."
                }
              ]
            }
            """;

        var result = _parser.ParseSkillMatch(response, 1);

        Assert.Single(result.Matches);
        Assert.Equal("Expert-level React skills with 60% capacity.", result.Matches[0].Reason);
    }

    [Fact]
    public void ParseTeamBuilder_ReturnsParsedDto_WhenValidJson()
    {
        var response = """
            {
              "roles": [
                {
                  "roleTitle": "Senior Java Developer",
                  "requiredSkills": [{"skillName": "Java", "minProficiency": "ADVANCED"}],
                  "status": "FILLED",
                  "assignedEmployeeName": "Mock Java Employee",
                  "matchScore": 90,
                  "reason": "Advanced Java on bench."
                }
              ]
            }
            """;

        var result = _parser.ParseTeamBuilder(response);

        Assert.Single(result.Roles);
        Assert.Equal("Senior Java Developer", result.Roles[0].RoleTitle);
        Assert.Equal(MockData.Names.TeamBuilderJavaEmployee, result.Roles[0].AssignedEmployeeName);
    }

    [Fact]
    public void ParseTeamBuilder_Throws_WhenInvalidJson()
    {
        Assert.Throws<ValidationAppException>(() => _parser.ParseTeamBuilder("not valid json"));
    }

    [Fact]
    public void ParseTeamBuilder_ParsesMatchScoreAsString()
    {
        var response = """
            {
              "roles": [
                {
                  "roleTitle": "Senior Java Developer",
                  "requiredSkills": [{"skillName": "Java", "minProficiency": "ADVANCED"}],
                  "status": "FILLED",
                  "assignedEmployeeName": "Mock Java Employee",
                  "matchScore": "90",
                  "reason": "Advanced Java on bench."
                }
              ]
            }
            """;

        var result = _parser.ParseTeamBuilder(response);

        Assert.Equal(90, result.Roles[0].MatchScore);
    }

    [Fact]
    public void ParseTeamBuilder_AllowsTrailingCommaAfterLastRole()
    {
        var response = """
            {
              "roles": [
                {
                  "roleTitle": "QA Tester",
                  "requiredSkills": [{"skillName": "Selenium", "minProficiency": "BEGINNER"}],
                  "status": "GAP",
                  "gap": {
                    "reasonType": "NO_SKILL",
                    "message": "No employee has Selenium skills."
                  }
                },
              ]
            }
            """;

        var result = _parser.ParseTeamBuilder(response);

        Assert.Single(result.Roles);
        Assert.Equal("QA Tester", result.Roles[0].RoleTitle);
    }

    [Fact]
    public void ParseTeamBuilder_StripsMarkdownWrapper()
    {
        var response = """
            ```json
            {
              "roles": [
                {
                  "roleTitle": "DevOps Engineer",
                  "requiredSkills": [{"skillName": "Docker", "minProficiency": "INTERMEDIATE"}],
                  "status": "FILLED",
                  "assignedEmployeeName": "Mock Allocated Team Employee",
                  "matchScore": 85,
                  "reason": "Docker skills on bench."
                }
              ]
            }
            ```
            """;

        var result = _parser.ParseTeamBuilder(response);

        Assert.Single(result.Roles);
        Assert.Equal("DevOps Engineer", result.Roles[0].RoleTitle);
    }

    [Fact]
    public void ParseTeamBuilder_CoalescesNullRolesToEmptyList()
    {
        var response = """{ "roles": null }""";

        var result = _parser.ParseTeamBuilder(response);

        Assert.NotNull(result.Roles);
        Assert.Empty(result.Roles);
    }
}
