using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Server.AI.Infrastructure;
using Server.Common.Errors;
using Server.Exceptions;

namespace Tests;

public class LlmHttpErrorHelperTests
{
    [Fact]
    public async Task ThrowIfNotSuccessAsync_ThrowsNotFoundMessage_For404()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"error":{"message":"Model not found"}}""")
        };

        var ex = await Assert.ThrowsAsync<AiServiceAppException>(() =>
            LlmHttpErrorHelper.ThrowIfNotSuccessAsync(response, "Gemini", NullLogger.Instance));

        Assert.Equal(ErrorCodes.LlmRequestFailed, ex.ErrorCode);
        Assert.Contains("model or endpoint not found", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Model not found", ex.Message);
    }

    [Fact]
    public async Task ThrowIfNotSuccessAsync_ThrowsUnauthorizedMessage_For401()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"error":{"message":"Invalid API key"}}""")
        };

        var ex = await Assert.ThrowsAsync<AiServiceAppException>(() =>
            LlmHttpErrorHelper.ThrowIfNotSuccessAsync(response, "Groq", NullLogger.Instance));

        Assert.Equal(ErrorCodes.LlmNotConfigured, ex.ErrorCode);
        Assert.Contains("invalid or unauthorized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ThrowIfNotSuccessAsync_DoesNotThrow_WhenSuccess()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        await LlmHttpErrorHelper.ThrowIfNotSuccessAsync(response, "Gemini", NullLogger.Instance);
    }
}
