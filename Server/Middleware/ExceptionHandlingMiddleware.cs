using System.Text.Json;
using Server.Common;
using Server.Common.Errors;
using Server.Exceptions;

namespace Server.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(
                    ex.Message,
                    ex.ErrorCode,
                    ex is ValidationAppException validation ? validation.Details : null),
                JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail("An unexpected error occurred.", ErrorCodes.UnexpectedError),
                JsonOptions);
        }
    }
}
