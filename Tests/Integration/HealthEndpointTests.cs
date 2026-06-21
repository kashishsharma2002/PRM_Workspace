using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Server.Data;
using System.Net;

namespace Tests.Integration;

public class HealthEndpointTests : IClassFixture<HealthEndpointTests.TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOkWithHealthyStatus()
    {
        var response = await _client.GetAsync("/health");

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"response body: {body}");
        body.Should().Contain("healthy");
        body.Should().Contain("PRM.Server");
    }

    public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=PrmTest;Trusted_Connection=True;",
                    ["JwtSettings:SecretKey"] = "IntegrationTestSecretKeyMustBeLongEnoughForHmacSha256",
                    ["JwtSettings:Issuer"] = "PRM.Server",
                    ["JwtSettings:Audience"] = "PRM.Client",
                    ["JwtSettings:ExpiryHours"] = "8",
                    ["LlmSettings:HttpTimeoutSeconds"] = "120",
                    ["LlmSettings:Gemini:BaseUrl"] = "https://generativelanguage.googleapis.com",
                    ["LlmSettings:Gemini:ApiVersion"] = "v1beta",
                    ["LlmSettings:Gemini:DefaultModel"] = "gemini-2.0-flash",
                    ["LlmSettings:Groq:BaseUrl"] = "https://api.groq.com",
                    ["LlmSettings:Groq:ChatCompletionsPath"] = "/openai/v1/chat/completions",
                    ["LlmSettings:Groq:DefaultModel"] = "llama-3.3-70b-versatile",
                    ["LlmSettings:Gemma:GenerateUrl"] = "http://localhost:11434/api/generate",
                    ["LlmSettings:Gemma:DefaultModel"] = "gemma3:12b-it-q8_0"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<PrmDbContext>>();
                services.RemoveAll<PrmDbContext>();

                services.AddDbContext<PrmDbContext>(options =>
                    options.UseInMemoryDatabase("HealthEndpointTests"));

                services.RemoveAll<IHostedService>();
            });
        }
    }
}
