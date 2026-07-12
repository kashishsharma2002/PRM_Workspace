using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Server.AI.Abstractions;
using Server.Exceptions;
using Server.Models.DTOs.ProjectRisk;
using Server.Models.Entities;
using Server.Repositories.AiRequestLog;
using Server.Repositories.Projects;
using Server.Repositories.SystemConfig;
using Server.Services.ProjectRisk;
using Server.Services.ProjectRisk.Abstractions;

namespace Tests.ProjectRisk;

public class ProjectRiskInsightsServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<ILlmClientFactory> _llmClientFactoryMock;
    private readonly Mock<ILlmClient> _llmClientMock;
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock;
    private readonly Mock<IAiRequestLogRepository> _aiRequestLogRepoMock;
    private readonly Mock<IProjectRiskContextAssembler> _contextAssemblerMock;
    private readonly ProjectRiskResponseParser _responseParser;
    private readonly ProjectRiskInsightsService _service;

    private readonly long _managerId = 1;
    private readonly long _projectId = 10;

    public ProjectRiskInsightsServiceTests()
    {
        _projectRepoMock = new Mock<IProjectRepository>();
        _llmClientFactoryMock = new Mock<ILlmClientFactory>();
        _llmClientMock = new Mock<ILlmClient>();
        _systemConfigRepoMock = new Mock<ISystemConfigRepository>();
        _aiRequestLogRepoMock = new Mock<IAiRequestLogRepository>();
        _contextAssemblerMock = new Mock<IProjectRiskContextAssembler>();

        _responseParser = new ProjectRiskResponseParser(NullLogger<ProjectRiskResponseParser>.Instance);
        var promptBuilder = new ProjectRiskPromptBuilder();

        _llmClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_llmClientMock.Object);

        _service = new ProjectRiskInsightsService(
            _projectRepoMock.Object,
            _llmClientFactoryMock.Object,
            _systemConfigRepoMock.Object,
            _aiRequestLogRepoMock.Object,
            _contextAssemblerMock.Object,
            promptBuilder,
            _responseParser,
            NullLogger<ProjectRiskInsightsService>.Instance);
    }

    [Fact]
    public async Task GetRiskSummaryAsync_ThrowsNotFound_WhenManagerDoesNotOwnProject()
    {
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = _projectId, ManagerUserId = 999 });

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            _service.GetRiskSummaryAsync(_managerId, _projectId));
    }
}
