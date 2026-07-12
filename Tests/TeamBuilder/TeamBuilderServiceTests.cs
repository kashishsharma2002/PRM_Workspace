using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Server.AI.Abstractions;
using Server.Exceptions;
using Server.Models.DTOs.Ai;
using Server.Models.DTOs.Ai.Context;
using Server.Models.DTOs.SkillMatching.Context;
using Server.Models.Entities;
using Server.Repositories.AiRequestLog;
using Server.Repositories.SystemConfig;
using Server.Services.TeamBuilder;
using Server.Services.TeamBuilder.Abstractions;

namespace Tests.TeamBuilder;

public class TeamBuilderServiceTests
{
    private readonly Mock<ILlmClientFactory> _llmClientFactoryMock;
    private readonly Mock<ILlmClient> _llmClientMock;
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock;
    private readonly Mock<IAiRequestLogRepository> _aiRequestLogRepoMock;
    private readonly Mock<ITeamBuilderContextBuilder> _contextBuilderMock;
    private readonly TeamBuilderResponseParser _responseParser;
    private readonly TeamBuilderResponseNormalizer _responseNormalizer;
    private readonly TeamBuilderService _service;

    private readonly long _managerId = 1;

    public TeamBuilderServiceTests()
    {
        _llmClientFactoryMock = new Mock<ILlmClientFactory>();
        _llmClientMock = new Mock<ILlmClient>();
        _systemConfigRepoMock = new Mock<ISystemConfigRepository>();
        _aiRequestLogRepoMock = new Mock<IAiRequestLogRepository>();
        _contextBuilderMock = new Mock<ITeamBuilderContextBuilder>();

        _responseParser = new TeamBuilderResponseParser(NullLogger<TeamBuilderResponseParser>.Instance);
        _responseNormalizer = new TeamBuilderResponseNormalizer();

        _llmClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_llmClientMock.Object);

        var promptBuilder = new TeamBuilderPromptBuilder();

        _service = new TeamBuilderService(
            _llmClientFactoryMock.Object,
            _systemConfigRepoMock.Object,
            _aiRequestLogRepoMock.Object,
            _contextBuilderMock.Object,
            _responseParser,
            _responseNormalizer,
            promptBuilder,
            NullLogger<TeamBuilderService>.Instance);
    }

    [Fact]
    public async Task BuildTeamAsync_ReturnsRoles_AndLogsRequest()
    {
        var contextModel = new AiTeamBuilderContextModel
        {
            AllCandidates = new List<AiSkillMatchCandidateContext>(),
            AssignableCandidates = new List<AiSkillMatchCandidateContext>()
        };

        _contextBuilderMock.Setup(b => b.BuildTeamBuilderRawContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contextModel);
        _contextBuilderMock.Setup(b => b.SerializeTeamBuilderContext(It.IsAny<AiTeamBuilderContextModel>()))
            .Returns("{}");

        _llmClientMock.Setup(c => c.GenerateCompletionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
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
                    }
                  ]
                }
                """);

        var result = await _service.BuildTeamAsync(_managerId, "For a banking portal we need a QA Tester with Selenium.");

        Assert.Single(result.Roles);
        Assert.Equal("QA Tester", result.Roles[0].RoleTitle);
        Assert.Equal("GAP", result.Roles[0].Status);
        _aiRequestLogRepoMock.Verify(r => r.AddAsync(It.Is<AiRequestLog>(l => l.RequestType == "TEAM_BUILDER"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildTeamAsync_RetriesOnce_WhenInitialParseFails()
    {
        var contextModel = new AiTeamBuilderContextModel
        {
            AllCandidates = new List<AiSkillMatchCandidateContext>(),
            AssignableCandidates = new List<AiSkillMatchCandidateContext>()
        };

        _contextBuilderMock.Setup(b => b.BuildTeamBuilderRawContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contextModel);
        _contextBuilderMock.Setup(b => b.SerializeTeamBuilderContext(It.IsAny<AiTeamBuilderContextModel>()))
            .Returns("{}");

        var callSeq = new Queue<string>(new[] {
            "not valid json",
            """
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
                }
              ]
            }
            """
        });

        _llmClientMock.Setup(c => c.GenerateCompletionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(callSeq.Dequeue);

        var result = await _service.BuildTeamAsync(_managerId, "For a banking portal we need a QA Tester with Selenium.");

        Assert.Single(result.Roles);
        Assert.Equal("GAP", result.Roles[0].Status);
        _llmClientMock.Verify(c => c.GenerateCompletionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task BuildTeamAsync_NormalizesDuplicateAssignment_ToGap()
    {
        var candidates = new List<AiSkillMatchCandidateContext>
        {
            new() { EmployeeId = 100, FullName = "Mock Team Builder Employee", RemainingCapacityPercentage = 100m }
        };
        var contextModel = new AiTeamBuilderContextModel
        {
            AllCandidates = candidates,
            AssignableCandidates = candidates
        };

        _contextBuilderMock.Setup(b => b.BuildTeamBuilderRawContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contextModel);
        _contextBuilderMock.Setup(b => b.SerializeTeamBuilderContext(It.IsAny<AiTeamBuilderContextModel>()))
            .Returns("{}");

        _llmClientMock.Setup(c => c.GenerateCompletionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "roles": [
                    {
                      "roleTitle": "Senior React Developer (1)",
                      "requiredSkills": [{"skillName": "React", "minProficiency": "ADVANCED"}],
                      "status": "FILLED",
                      "assignedEmployeeName": "Mock Team Builder Employee",
                      "matchScore": 90,
                      "reason": "React expert on bench."
                    },
                    {
                      "roleTitle": "Senior React Developer (2)",
                      "requiredSkills": [{"skillName": "React", "minProficiency": "ADVANCED"}],
                      "status": "FILLED",
                      "assignedEmployeeName": "Mock Team Builder Employee",
                      "matchScore": 85,
                      "reason": "Only React developer available."
                    }
                  ]
                }
                """);

        var result = await _service.BuildTeamAsync(_managerId, "Banking portal with 2 Senior React Developers.");

        Assert.Equal(2, result.Roles.Count);
        Assert.Equal("FILLED", result.Roles[0].Status);
        Assert.Equal("GAP", result.Roles[1].Status);
        Assert.Equal("ALREADY_ASSIGNED_IN_TEAM", result.Roles[1].Gap?.ReasonType);
    }
}
