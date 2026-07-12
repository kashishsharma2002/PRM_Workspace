using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Server.Exceptions;
using Server.AI.Abstractions;
using Server.Models.DTOs.SkillMatching;
using Server.Models.DTOs.SkillMatching.Context;
using Server.Models.Entities;
using Server.Repositories.AiRequestLog;
using Server.Repositories.Projects;
using Server.Repositories.SystemConfig;
using Server.Services.SkillMatching;
using Server.Services.SkillMatching.Abstractions;

namespace Tests.SkillMatching;

public class SkillMatchingServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<ILlmClientFactory> _llmClientFactoryMock;
    private readonly Mock<ILlmClient> _llmClientMock;
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock;
    private readonly Mock<IAiRequestLogRepository> _aiRequestLogRepoMock;
    private readonly Mock<ISkillMatchContextAssembler> _contextAssemblerMock;
    private readonly SkillMatchResponseParser _responseParser;
    private readonly SkillMatchCandidateFilter _candidateFilter;
    private readonly SkillMatchRanker _ranker;
    private readonly ProjectHealthResourceFilter _projectHealthResourceFilter;
    private readonly SkillMatchingService _service;

    private readonly long _managerId = 1;
    private readonly long _projectId = 10;

    public SkillMatchingServiceTests()
    {
        _projectRepoMock = new Mock<IProjectRepository>();
        _llmClientFactoryMock = new Mock<ILlmClientFactory>();
        _llmClientMock = new Mock<ILlmClient>();
        _systemConfigRepoMock = new Mock<ISystemConfigRepository>();
        _aiRequestLogRepoMock = new Mock<IAiRequestLogRepository>();
        _contextAssemblerMock = new Mock<ISkillMatchContextAssembler>();

        _responseParser = new SkillMatchResponseParser(NullLogger<SkillMatchResponseParser>.Instance);
        _candidateFilter = new SkillMatchCandidateFilter(NullLogger<SkillMatchCandidateFilter>.Instance);
        _ranker = new SkillMatchRanker(_candidateFilter, NullLogger<SkillMatchRanker>.Instance);
        _projectHealthResourceFilter = new ProjectHealthResourceFilter();

        _llmClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_llmClientMock.Object);

        var promptBuilder = new SkillMatchPromptBuilder();

        _service = new SkillMatchingService(
            _projectRepoMock.Object,
            _llmClientFactoryMock.Object,
            _systemConfigRepoMock.Object,
            _aiRequestLogRepoMock.Object,
            _contextAssemblerMock.Object,
            _responseParser,
            promptBuilder,
            _candidateFilter,
            _ranker,
            _projectHealthResourceFilter,
            NullLogger<SkillMatchingService>.Instance);
    }

    [Fact]
    public async Task GetSkillMatchAsync_ThrowsValidation_WhenRequirementTooLong()
    {
        var longRequirement = new string('x', 501);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _service.GetSkillMatchAsync(_managerId, _projectId, longRequirement));
    }

    [Fact]
    public async Task GetSkillMatchAsync_ThrowsNotFound_WhenManagerDoesNotOwnProject()
    {
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = _projectId, ManagerUserId = 999 });

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            _service.GetSkillMatchAsync(_managerId, _projectId, "Need a developer"));
    }

    [Fact]
    public async Task GetOrganizationalSkillMatchAsync_ReturnsMatches_WithoutProjectOwnership()
    {
        var requirement = "Need a React developer";
        var candidates = new List<AiSkillMatchCandidateContext>
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
        };

        _contextAssemblerMock.Setup(a => a.AssembleCandidatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

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

        var result = await _service.GetOrganizationalSkillMatchAsync(_managerId, requirement);

        Assert.Equal(0, result.ProjectId);
        Assert.Single(result.Matches);
        Assert.Equal("Mock Org Employee", result.Matches[0].EmployeeName);
        _aiRequestLogRepoMock.Verify(r => r.AddAsync(It.IsAny<AiRequestLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSkillMatchAsync_ReturnsMatches_AndLogsRequest()
    {
        var project = new Project { Id = _projectId, ManagerUserId = _managerId, ProjectName = "Test" };
        var requirement = "Need a C# developer";
        var candidates = new List<AiSkillMatchCandidateContext>
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
        };

        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _contextAssemblerMock.Setup(a => a.AssembleCandidatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

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

        var result = await _service.GetSkillMatchAsync(_managerId, _projectId, requirement);

        Assert.Single(result.Matches);
        Assert.Equal("Mock Test Employee", result.Matches[0].EmployeeName);
        _aiRequestLogRepoMock.Verify(r => r.AddAsync(It.IsAny<AiRequestLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
