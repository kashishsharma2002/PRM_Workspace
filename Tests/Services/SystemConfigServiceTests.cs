using Microsoft.EntityFrameworkCore;
using Tests.Helpers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Server.Common;
using Server.Data;
using Server.Models.DTOs.SystemConfig;
using Server.Models.Entities;

namespace Tests;

public class SystemConfigServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly SystemConfigService _systemConfigService;

    public SystemConfigServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        SeedConfig();

        var dataProtection = DataProtectionProvider.Create("Tests");
        var encryption = new ConfigEncryptionHelper(dataProtection);
        _systemConfigService = new SystemConfigService(
            new SystemConfigRepository(_context),
            TestServiceFactory.CreateAuditService(_context),
            encryption,
            TestServiceFactory.CreateLogger<SystemConfigService>());
    }

    private void SeedConfig()
    {
        var now = DateTime.UtcNow;
        _context.SystemConfigurations.AddRange(
            new SystemConfiguration { ConfigKey = ConfigKeys.LlmProvider, ConfigValue = "Gemini", UpdatedAt = now },
            new SystemConfiguration { ConfigKey = ConfigKeys.LlmApiKey, ConfigValue = "", UpdatedAt = now },
            new SystemConfiguration { ConfigKey = ConfigKeys.SchedulerIntervalHours, ConfigValue = "4", UpdatedAt = now },
            new SystemConfiguration { ConfigKey = ConfigKeys.MaxWeeklyHours, ConfigValue = "40", UpdatedAt = now });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetConfigAsync_MasksApiKeyWhenSet()
    {
        var config = await _context.SystemConfigurations.FirstAsync(c => c.ConfigKey == ConfigKeys.LlmApiKey);
        config.ConfigValue = "plain-key";
        await _context.SaveChangesAsync();

        var result = await _systemConfigService.GetConfigAsync();

        Assert.Equal("****************************", result.LlmApiKeyMasked);
    }

    [Fact]
    public async Task UpdateConfigAsync_EncryptsApiKey()
    {
        await _systemConfigService.UpdateConfigAsync(1, new UpdateSystemConfigRequestDto
        {
            LlmApiKey = "my-secret-key-123"
        });

        var stored = await _context.SystemConfigurations.FirstAsync(c => c.ConfigKey == ConfigKeys.LlmApiKey);
        Assert.NotEqual("my-secret-key-123", stored.ConfigValue);
        Assert.False(string.IsNullOrWhiteSpace(stored.ConfigValue));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
