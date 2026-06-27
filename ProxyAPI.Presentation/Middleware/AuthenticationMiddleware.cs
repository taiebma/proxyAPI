namespace ProxyAPI.Presentation.Middleware;

using ProxyAPI.Application.Interfaces;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string ClientIdCookieName = "X-ProxyAPI-ClientId";

    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthenticationService authService)
    {
        if (!context.Request.Path.StartsWithSegments("/api/auth"))
        {
            if (context.Request.Headers.TryGetValue(ClientIdCookieName, out var clientId))
            {
                var clientContext = await authService.GetClientContextAsync(clientId);

                if (clientContext != null)
                {
                    context.Items["ClientContext"] = clientContext;
                    context.Request.Headers.Authorization = $"Bearer {clientContext.AccessToken}";
                }
                else
                {
                    var refreshedContext = await authService.RefreshClientContextAsync(clientId);
                    if (refreshedContext != null)
                    {
                        context.Items["ClientContext"] = refreshedContext;
                        context.Request.Headers.Authorization = $"Bearer {refreshedContext.AccessToken}";
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired session" });
                        return;
                    }
                }
            }
            else
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
