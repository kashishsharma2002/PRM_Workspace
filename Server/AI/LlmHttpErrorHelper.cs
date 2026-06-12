using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Server.Common.Errors;
using Server.Exceptions;

namespace Server.AI;

public static class LlmHttpErrorHelper
{
    public static async Task ThrowIfNotSuccessAsync(
        HttpResponseMessage response,
        string providerName,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogError(
            "{Provider} API returned {StatusCode}: {Body}",
            providerName,
            (int)response.StatusCode,
            body);

        var message = BuildUserMessage(response.StatusCode, providerName, body);
        var errorCode = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            ? ErrorCodes.LlmNotConfigured
            : ErrorCodes.LlmRequestFailed;

        throw new AiServiceAppException(message, errorCode);
    }

    private static string BuildUserMessage(HttpStatusCode statusCode, string providerName, string body)
    {
        var providerDetail = ExtractProviderMessage(body);

        return statusCode switch
        {
            HttpStatusCode.NotFound =>
                $"{providerName} model or endpoint not found — check model name.{FormatDetail(providerDetail)}",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                $"{providerName} API key is invalid or unauthorized.{FormatDetail(providerDetail)}",
            HttpStatusCode.BadRequest =>
                $"{providerName} rejected the request.{FormatDetail(providerDetail)}",
            _ =>
                $"{providerName} request failed ({(int)statusCode} {statusCode}).{FormatDetail(providerDetail)}"
        };
    }

    private static string ExtractProviderMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                    return message.GetString() ?? string.Empty;

                if (error.ValueKind == JsonValueKind.String)
                    return error.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("message", out var topMessage))
                return topMessage.GetString() ?? string.Empty;
        }
        catch (JsonException)
        {
            if (body.Length <= 200)
                return body;
        }

        return string.Empty;
    }

    private static string FormatDetail(string detail) =>
        string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}";
}
