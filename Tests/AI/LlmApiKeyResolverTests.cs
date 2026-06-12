using Moq;
using Server.AI.Configuration;
using Server.AI.Infrastructure;
using Server.Common;
using Server.Exceptions;
using Server.Models.Entities;
using Server.Repositories.SystemConfig;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.AI;

public class LlmApiKeyResolverTests
{
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock;
    private readonly Mock<IConfigEncryptionHelper> _encryptionMock;
    private readonly LlmApiKeyResolver _resolver;

    public LlmApiKeyResolverTests()
    {
        _systemConfigRepoMock = new Mock<ISystemConfigRepository>();
        _encryptionMock = new Mock<IConfigEncryptionHelper>();
        _resolver = new LlmApiKeyResolver(_encryptionMock.Object);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsDecryptedKey_WhenStoredEncrypted()
    {
        // Arrange
        var config = new SystemConfiguration { ConfigKey = ConfigKeys.LlmApiKey, ConfigValue = "enc:AIzaSyTestKey" };
        _systemConfigRepoMock.Setup(r => r.GetByKeyAsync(ConfigKeys.LlmApiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _encryptionMock.Setup(e => e.IsEncrypted("enc:AIzaSyTestKey")).Returns(true);
        _encryptionMock.Setup(e => e.Decrypt("enc:AIzaSyTestKey")).Returns("AIzaSyTestKey");

        // Act
        var resolved = await _resolver.ResolveAsync(_systemConfigRepoMock.Object, LlmProviderKeys.Gemma);

        // Assert
        Assert.Equal("AIzaSyTestKey", resolved);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsPlainText_WhenStoredUnencrypted()
    {
        // Arrange
        var config = new SystemConfiguration { ConfigKey = ConfigKeys.LlmApiKey, ConfigValue = "plain-api-key" };
        _systemConfigRepoMock.Setup(r => r.GetByKeyAsync(ConfigKeys.LlmApiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _encryptionMock.Setup(e => e.IsEncrypted("plain-api-key")).Returns(false);

        // Act
        var resolved = await _resolver.ResolveAsync(_systemConfigRepoMock.Object, LlmProviderKeys.Gemma);

        // Assert
        Assert.Equal("plain-api-key", resolved);
    }

    [Fact]
    public async Task ResolveAsync_Throws_WhenEncryptedValueCannotBeDecrypted()
    {
        // Arrange
        var config = new SystemConfiguration { ConfigKey = ConfigKeys.LlmApiKey, ConfigValue = "enc:invalid-protected-blob" };
        _systemConfigRepoMock.Setup(r => r.GetByKeyAsync(ConfigKeys.LlmApiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _encryptionMock.Setup(e => e.IsEncrypted("enc:invalid-protected-blob")).Returns(true);
        _encryptionMock.Setup(e => e.Decrypt("enc:invalid-protected-blob")).Throws(new Exception("Decryption failed"));

        // Act & Assert
        await Assert.ThrowsAsync<AiServiceAppException>(() =>
            _resolver.ResolveAsync(_systemConfigRepoMock.Object, LlmProviderKeys.Gemma));
    }
}
