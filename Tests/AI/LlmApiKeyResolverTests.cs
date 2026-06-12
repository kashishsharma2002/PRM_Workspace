using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.AI;
using Server.Common;
using Server.Data;
using Server.Exceptions;
using Server.Models.Entities;
using Server.Repositories.SystemConfig;
using Tests.Helpers;

namespace Tests;

public class LlmApiKeyResolverTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly ConfigEncryptionHelper _encryption = new(DataProtectionProvider.Create("Tests"));

    public LlmApiKeyResolverTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        _context.SystemConfigurations.Add(new SystemConfiguration
        {
            ConfigKey = ConfigKeys.LlmApiKey,
            ConfigValue = string.Empty,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task ResolveAsync_ReturnsDecryptedKey_WhenStoredEncrypted()
    {
        var config = await _context.SystemConfigurations.FirstAsync(c => c.ConfigKey == ConfigKeys.LlmApiKey);
        config.ConfigValue = _encryption.Encrypt("AIzaSyTestKey");
        await _context.SaveChangesAsync();

        var repository = new SystemConfigRepository(_context);
        var resolved = await LlmApiKeyResolver.ResolveAsync(repository, _encryption);

        Assert.Equal("AIzaSyTestKey", resolved);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsPlainText_WhenStoredUnencrypted()
    {
        var config = await _context.SystemConfigurations.FirstAsync(c => c.ConfigKey == ConfigKeys.LlmApiKey);
        config.ConfigValue = "plain-api-key";
        await _context.SaveChangesAsync();

        var repository = new SystemConfigRepository(_context);
        var resolved = await LlmApiKeyResolver.ResolveAsync(repository, _encryption);

        Assert.Equal("plain-api-key", resolved);
    }

    [Fact]
    public async Task ResolveAsync_Throws_WhenEncryptedValueCannotBeDecrypted()
    {
        var config = await _context.SystemConfigurations.FirstAsync(c => c.ConfigKey == ConfigKeys.LlmApiKey);
        config.ConfigValue = ConfigEncryptionHelper.EncryptedPrefix + "invalid-protected-blob";
        await _context.SaveChangesAsync();

        var repository = new SystemConfigRepository(_context);

        await Assert.ThrowsAsync<AiServiceAppException>(() =>
            LlmApiKeyResolver.ResolveAsync(repository, _encryption));
    }

    public void Dispose() => _context.Dispose();
}
