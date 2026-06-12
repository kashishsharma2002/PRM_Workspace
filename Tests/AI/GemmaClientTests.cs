using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Server.AI.Abstractions;
using Server.AI.Configuration;
using Server.AI.Infrastructure;
using Server.AI.Providers;
using Server.Common;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.SystemConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.AI;

public class GemmaClientTests : IDisposable
{
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock;
    private readonly Mock<ILlmApiKeyResolver> _apiKeyResolverMock;
    private readonly Mock<ILlmConfigResolver> _configResolverMock;
    private readonly CapturingHandler _handler = new();
    private readonly GemmaClient _client;
    private readonly LlmSettings _llmSettings = new();

    public GemmaClientTests()
    {
        _systemConfigRepoMock = new Mock<ISystemConfigRepository>();
        _apiKeyResolverMock = new Mock<ILlmApiKeyResolver>();
        _configResolverMock = new Mock<ILlmConfigResolver>();

        _configResolverMock.Setup(r => r.ResolveModelAsync(
            It.IsAny<ISystemConfigRepository>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(async (ISystemConfigRepository repo, string key, string defModel, CancellationToken ct) =>
            {
                var config = await repo.GetByKeyAsync(key, ct);
                return string.IsNullOrWhiteSpace(config?.ConfigValue) ? defModel : config.ConfigValue.Trim();
            });

        var httpClientFactory = new StubHttpClientFactory(new HttpClient(_handler));

        _client = new GemmaClient(
            httpClientFactory,
            _systemConfigRepoMock.Object,
            _apiKeyResolverMock.Object,
            _configResolverMock.Object,
            Options.Create(_llmSettings),
            NullLogger<GemmaClient>.Instance);
    }

    [Fact]
    public async Task GenerateCompletionAsync_SendsEmptyApiKeyHeader_WhenKeyNotConfigured()
    {
        // Arrange
        _apiKeyResolverMock.Setup(r => r.ResolveAsync(_systemConfigRepoMock.Object, LlmProviderKeys.Gemma, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        _systemConfigRepoMock.Setup(r => r.GetByKeyAsync(ConfigKeys.LlmModelGemma, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemConfiguration?)null);

        // Act
        var result = await _client.GenerateCompletionAsync("Hello");

        // Assert
        Assert.Equal("Hello there!", result);
        Assert.NotNull(_handler.LastRequest);
        Assert.True(_handler.LastRequest!.Headers.TryGetValues("apikey", out var values));
        Assert.Equal(string.Empty, values!.Single());

        var body = _handler.LastRequestBody!;
        using var json = JsonDocument.Parse(body);
        Assert.Equal(_llmSettings.Gemma.DefaultModel, json.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task GenerateCompletionAsync_SendsDecryptedApiKeyHeader_WhenKeyConfigured()
    {
        // Arrange
        _apiKeyResolverMock.Setup(r => r.ResolveAsync(_systemConfigRepoMock.Object, LlmProviderKeys.Gemma, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ollama-secret");
        _systemConfigRepoMock.Setup(r => r.GetByKeyAsync(ConfigKeys.LlmModelGemma, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemConfiguration?)null);

        // Act
        await _client.GenerateCompletionAsync("Hello");

        // Assert
        Assert.True(_handler.LastRequest!.Headers.TryGetValues("apikey", out var values));
        Assert.Equal("ollama-secret", values!.Single());
    }

    [Fact]
    public async Task GenerateCompletionAsync_UsesConfiguredModel_WhenSetInSystemConfig()
    {
        // Arrange
        _apiKeyResolverMock.Setup(r => r.ResolveAsync(_systemConfigRepoMock.Object, LlmProviderKeys.Gemma, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        _systemConfigRepoMock.Setup(r => r.GetByKeyAsync(ConfigKeys.LlmModelGemma, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemConfiguration { ConfigKey = ConfigKeys.LlmModelGemma, ConfigValue = "custom-gemma-model" });

        // Act
        await _client.GenerateCompletionAsync("Hello");

        // Assert
        var body = _handler.LastRequestBody!;
        using var json = JsonDocument.Parse(body);
        Assert.Equal("custom-gemma-model", json.RootElement.GetProperty("model").GetString());
    }

    public void Dispose()
    {
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
