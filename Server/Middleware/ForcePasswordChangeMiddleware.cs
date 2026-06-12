using System.Text.Json;
using Server.Common;

namespace Server.Middleware;

public class ForcePasswordChangeMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.User.HasClaim("force_password_change", "true")
            && !IsChangePasswordEndpoint(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail("Password change required."),
                JsonOptions);
            return;
        }

        await next(context);
    }

    private static bool IsChangePasswordEndpoint(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/api/auth/change-password", StringComparison.OrdinalIgnoreCase)
        && HttpMethods.IsPost(context.Request.Method);
}
