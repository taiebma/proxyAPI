namespace ProxyAPI.Presentation.Middleware;

using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Infrastructure.Configuration;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string ClientIdCookieName = "X-ProxyAPI-ClientId";

    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/auth"))
        {
            if (!context.Request.Headers.TryGetValue(ClientIdCookieName, out var clientId) || string.IsNullOrWhiteSpace(clientId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "No session cookie found" });
                return;
            }
        }

        await _next(context);
    }
}

public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthenticationMiddleware>();
    }
}
