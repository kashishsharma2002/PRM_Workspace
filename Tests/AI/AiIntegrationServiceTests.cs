using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.AI.Infrastructure;
using Server.Common;
using Server.Common.Ai;
using Server.Exceptions;
using Server.Models.DTOs.Ai;
using Server.Models.DTOs.Ai.Context;
using Server.Models.Entities;
using Server.Repositories.Ai;
using Server.Repositories.Projects;
using Server.Repositories.SystemConfig;
using Server.Services.Ai;
using Server.Services.Ai.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.AI;

public class AiIntegrationServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<ILlmClientFactory> _llmClientFactoryMock;
    private readonly Mock<ILlmClient> _llmClientMock;
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock;
    private readonly Mock<IAiRequestLogRepository> _aiRequestLogRepoMock;
    private readonly Mock<IAiContextBuilder> _contextBuilderMock;
    private readonly AiResponseParser _responseParser;
    private readonly TeamBuilderResponseNormalizer _responseNormalizer;
    private readonly SkillMatchCandidateFilter _candidateFilter;
    private readonly SkillMatchRanker _ranker;
    private readonly AiIntegrationService _service;

    private readonly long _managerId = 1;
    private readonly long _projectId = 10;

    public AiIntegrationServiceTests()
    {
        _projectRepoMock = new Mock<IProjectRepository>();
        _llmClientFactoryMock = new Mock<ILlmClientFactory>();
        _llmClientMock = new Mock<ILlmClient>();
        _systemConfigRepoMock = new Mock<ISystemConfigRepository>();
        _aiRequestLogRepoMock = new Mock<IAiRequestLogRepository>();
        _contextBuilderMock = new Mock<IAiContextBuilder>();

        _responseParser = new AiResponseParser(NullLogger<AiResponseParser>.Instance);
        _responseNormalizer = new TeamBuilderResponseNormalizer();
        _candidateFilter = new SkillMatchCandidateFilter(NullLogger<SkillMatchCandidateFilter>.Instance);
        _ranker = new SkillMatchRanker(_candidateFilter, NullLogger<SkillMatchRanker>.Instance);
        var projectHealthResourceFilter = new ProjectHealthResourceFilter();

        _llmClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_llmClientMock.Object);

        var promptBuilderMock = new Mock<IAiPromptBuilder>();

        _service = new AiIntegrationService(
            _projectRepoMock.Object,
            _llmClientFactoryMock.Object,
            _systemConfigRepoMock.Object,
            _aiRequestLogRepoMock.Object,
            _contextBuilderMock.Object,
            _responseParser,
            _responseNormalizer,
            promptBuilderMock.Object,
            _candidateFilter,
            _ranker,
            projectHealthResourceFilter,
            NullLogger<AiIntegrationService>.Instance);
    }

    [Fact]
    public async Task GetRiskSummaryAsync_ThrowsNotFound_WhenManagerDoesNotOwnProject()
    {
        // Arrange
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = _projectId, ManagerUserId = 999 }); // Different manager

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            _service.GetRiskSummaryAsync(_managerId, _projectId));
    }

    [Fact]
    public async Task GetSkillMatchAsync_ThrowsValidation_WhenRequirementTooLong()
    {
        // Arrange
        var longRequirement = new string('x', 501);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _service.GetSkillMatchAsync(_managerId, _projectId, longRequirement));
    }

    [Fact]
    public async Task GetSkillMatchAsync_ThrowsNotFound_WhenManagerDoesNotOwnProject()
    {
        // Arrange
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = _projectId, ManagerUserId = 999 });

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            _service.GetSkillMatchAsync(_managerId, _projectId, "Need a developer"));
    }

    [Fact]
    public async Task GetOrganizationalSkillMatchAsync_ReturnsMatches_WithoutProjectOwnership()
    {
        // Arrange
        var requirement = "Need a React developer";
        var contextModel = new AiOrganizationalSkillMatchContextModel
        {
            Candidates = new List<AiSkillMatchCandidateContext>
            {
                new()
                {
                    EmployeeId = 100,
                    FullName = "Mock Org Employee",
                    Skills = new List<AiSkillContext>
                    {
                        new() { SkillName = "React", ProficiencyLevel = "ADVANCED" }
                    }
                }
            }
        };

        _contextBuilderMock.Setup(b => b.BuildOrganizationalSkillMatchContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((contextModel, "{}"));

        _llmClientMock.Setup(c => c.GenerateCompletionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "projectId": 0,
                  "matches": [
                    {
                      "employeeName": "Mock Org Employee",
                      "skillName": "React",
                      "matchScore": 91,
                      "reason": "Available React developer"
                    }
                  ]
                }
                """);

        // Act
        var result = await _service.GetOrganizationalSkillMatchAsync(_managerId, requirement);

        // Assert
        Assert.Equal(0, result.ProjectId);
        Assert.Single(result.Matches);
        Assert.Equal("Mock Org Employee", result.Matches[0].EmployeeName);
        _aiRequestLogRepoMock.Verify(r => r.AddAsync(It.IsAny<AiRequestLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSkillMatchAsync_ReturnsMatches_AndLogsRequest()
    {
        // Arrange
        var project = new Project { Id = _projectId, ManagerUserId = _managerId };
        var requirement = "Need a C# developer";
        var contextModel = new AiSkillMatchContextModel
        {
            Candidates = new List<AiSkillMatchCandidateContext>
            {
                new()
                {
                    EmployeeId = 100,
                    FullName = "Mock Test Employee",
                    Skills = new List<AiSkillContext>
                    {
                        new() { SkillName = "C#", ProficiencyLevel = "ADVANCED" }
                    }
                }
            }
        };

        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _contextBuilderMock.Setup(b => b.BuildSkillMatchContextAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((contextModel, "{}"));

        _llmClientMock.Setup(c => c.GenerateCompletionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                  "projectId": 10,
                  "matches": [
                    {
                      "employeeName": "Mock Test Employee",
                      "skillName": "C#",
                      "matchScore": 88,
                      "reason": "Strong match"
                    }
                  ]
                }
                """);

        // Act
        var result = await _service.GetSkillMatchAsync(_managerId, _projectId, requirement);

        // Assert
        Assert.Single(result.Matches);
        Assert.Equal("Mock Test Employee", result.Matches[0].EmployeeName);
        _aiRequestLogRepoMock.Verify(r => r.AddAsync(It.IsAny<AiRequestLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildTeamAsync_ReturnsRoles_AndLogsRequest()
    {
        // Arrange
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

        // Act
        var result = await _service.BuildTeamAsync(_managerId, "For a banking portal we need a QA Tester with Selenium.");

        // Assert
        Assert.Single(result.Roles);
        Assert.Equal("QA Tester", result.Roles[0].RoleTitle);
        Assert.Equal("GAP", result.Roles[0].Status);
        _aiRequestLogRepoMock.Verify(r => r.AddAsync(It.Is<AiRequestLog>(l => l.RequestType == "TEAM_BUILDER"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildTeamAsync_RetriesOnce_WhenInitialParseFails()
    {
        // Arrange
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

        // Act
        var result = await _service.BuildTeamAsync(_managerId, "For a banking portal we need a QA Tester with Selenium.");

        // Assert
        Assert.Single(result.Roles);
        Assert.Equal("GAP", result.Roles[0].Status);
        _llmClientMock.Verify(c => c.GenerateCompletionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task BuildTeamAsync_NormalizesDuplicateAssignment_ToGap()
    {
        // Arrange
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

        // Act
        var result = await _service.BuildTeamAsync(_managerId, "Banking portal with 2 Senior React Developers.");

        // Assert
        Assert.Equal(2, result.Roles.Count);
        Assert.Equal("FILLED", result.Roles[0].Status);
        Assert.Equal("GAP", result.Roles[1].Status);
        Assert.Equal("ALREADY_ASSIGNED_IN_TEAM", result.Roles[1].Gap?.ReasonType);
    }
}
