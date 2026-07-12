using Server.Exceptions;
using Server.Models.DTOs.SkillMatching;
using Server.Services.SkillMatching;
using Tests.Helpers;

namespace Tests.SkillMatching;

public class SkillMatchResponseParserTests
{
    private readonly SkillMatchResponseParser _parser =
        new(TestServiceFactory.CreateLogger<SkillMatchResponseParser>());

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
}
