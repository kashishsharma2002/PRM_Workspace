using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Audit;
using Server.Models.DTOs.SystemConfig;
using Server.Models.Entities;
using Server.Services.Shared;
using Server.Services.SystemConfig;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class SystemConfigServiceTests
{
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IConfigEncryptionHelper> _encryptionMock;
    private readonly Mock<ILogger<SystemConfigService>> _loggerMock;
    private readonly SystemConfigService _systemConfigService;

    public SystemConfigServiceTests()
    {
        _systemConfigRepoMock = new Mock<ISystemConfigRepository>();
        _auditServiceMock = new Mock<IAuditService>();
        _encryptionMock = new Mock<IConfigEncryptionHelper>();
        _loggerMock = new Mock<ILogger<SystemConfigService>>();

        _systemConfigService = new SystemConfigService(
            _systemConfigRepoMock.Object,
            _auditServiceMock.Object,
            _encryptionMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetConfigAsync_MasksApiKeyWhenSet()
    {
        // Arrange
        var configs = new List<SystemConfiguration>
        {
            new() { ConfigKey = ConfigKeys.LlmProvider, ConfigValue = "Gemini" },
            new() { ConfigKey = ConfigKeys.LlmApiKey, ConfigValue = "plain-key" },
            new() { ConfigKey = ConfigKeys.SchedulerIntervalHours, ConfigValue = "4" },
            new() { ConfigKey = ConfigKeys.MaxWeeklyHours, ConfigValue = "40" }
        };
        _systemConfigRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configs);

        // Act
        var result = await _systemConfigService.GetConfigAsync();

        // Assert
        Assert.Equal("****************************", result.LlmApiKeyMasked);
    }

    [Fact]
    public async Task UpdateConfigAsync_EncryptsApiKey()
    {
        // Arrange
        var config = new SystemConfiguration { ConfigKey = ConfigKeys.LlmApiKey, ConfigValue = "" };
        _systemConfigRepoMock.Setup(r => r.GetByKeyAsync(ConfigKeys.LlmApiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _encryptionMock.Setup(e => e.Encrypt("my-secret-key-123"))
            .Returns("ENC:my-secret-key-123");

        // Act
        await _systemConfigService.UpdateConfigAsync(1, new UpdateSystemConfigRequestDto
        {
            LlmApiKey = "my-secret-key-123"
        });

        // Assert
        Assert.Equal("ENC:my-secret-key-123", config.ConfigValue);
        _systemConfigRepoMock.Verify(r => r.UpdateAsync(config, It.IsAny<CancellationToken>()), Times.Once);
        _systemConfigRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogUpdateAsync(
            1,
            AuditEntityConstants.SystemConfigurations,
            0,
            null,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConfigAsync_ClearsApiKey_WhenEmptyStringProvided()
    {
        // Arrange
        var config = new SystemConfiguration { ConfigKey = ConfigKeys.LlmApiKey, ConfigValue = "old-key" };
        _systemConfigRepoMock.Setup(r => r.GetByKeyAsync(ConfigKeys.LlmApiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        await _systemConfigService.UpdateConfigAsync(1, new UpdateSystemConfigRequestDto
        {
            LlmApiKey = string.Empty
        });

        // Assert
        Assert.Equal(string.Empty, config.ConfigValue);
        _systemConfigRepoMock.Verify(r => r.UpdateAsync(config, It.IsAny<CancellationToken>()), Times.Once);
        _systemConfigRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
