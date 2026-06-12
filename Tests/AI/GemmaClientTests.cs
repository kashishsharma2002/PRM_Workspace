using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Server.AI;
using Server.Common;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.SystemConfig;
using Tests.Helpers;

namespace Tests;

public class GemmaClientTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly ConfigEncryptionHelper _encryption = new(DataProtectionProvider.Create("Tests"));
    private readonly CapturingHandler _handler = new();
    private readonly GemmaClient _client;

    public GemmaClientTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        SeedConfig();

        var httpClientFactory = new StubHttpClientFactory(new HttpClient(_handler));
        var repository = new SystemConfigRepository(_context);
        _client = new GemmaClient(httpClientFactory, repository, _encryption, NullLogger<GemmaClient>.Instance);
    }

    [Fact]
    public async Task GenerateCompletionAsync_SendsEmptyApiKeyHeader_WhenKeyNotConfigured()
    {
        var result = await _client.GenerateCompletionAsync("Hello");

        Assert.Equal("Hello there!", result);
        Assert.NotNull(_handler.LastRequest);
        Assert.True(_handler.LastRequest!.Headers.TryGetValues("apikey", out var values));
        Assert.Equal(string.Empty, values!.Single());

        var body = _handler.LastRequestBody!;
        using var json = JsonDocument.Parse(body);
        Assert.Equal(LlmDefaults.GemmaModel, json.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task GenerateCompletionAsync_SendsDecryptedApiKeyHeader_WhenKeyConfigured()
    {
        var config = await _context.SystemConfigurations.FirstAsync(c => c.ConfigKey == ConfigKeys.LlmApiKey);
        config.ConfigValue = _encryption.Encrypt("ollama-secret");
        await _context.SaveChangesAsync();

        await _client.GenerateCompletionAsync("Hello");

        Assert.True(_handler.LastRequest!.Headers.TryGetValues("apikey", out var values));
        Assert.Equal("ollama-secret", values!.Single());
    }

    [Fact]
    public async Task GenerateCompletionAsync_UsesConfiguredModel_WhenSetInSystemConfig()
    {
        _context.SystemConfigurations.Add(new SystemConfiguration
        {
            ConfigKey = ConfigKeys.LlmModelGemma,
            ConfigValue = "custom-gemma-model",
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _client.GenerateCompletionAsync("Hello");

        var body = _handler.LastRequestBody!;
        using var json = JsonDocument.Parse(body);
        Assert.Equal("custom-gemma-model", json.RootElement.GetProperty("model").GetString());
    }

    private void SeedConfig()
    {
        var now = DateTime.UtcNow;
        _context.SystemConfigurations.AddRange(
            new SystemConfiguration { ConfigKey = ConfigKeys.LlmApiKey, ConfigValue = string.Empty, UpdatedAt = now },
            new SystemConfiguration { ConfigKey = ConfigKeys.LlmProvider, ConfigValue = LlmProviders.Gemma, UpdatedAt = now });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _handler.Dispose();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"response":"Hello there!"}""", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
