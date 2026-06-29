namespace ProxyAPI.Presentation.Controllers;

using Microsoft.AspNetCore.Mvc;
using ProxyAPI.Domain.DTOs;
using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Infrastructure.Configuration;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly OIdcAuthSettings _oIdcAuthSettings;
    private const string ClientIdCookieName = "X-ProxyAPI-ClientId";

    public AuthController(IAuthenticationService authService, OIdcAuthSettings oIdcAuthSettings)
    {
        _authService = authService;
        _oIdcAuthSettings = oIdcAuthSettings;
    }

    [HttpGet("login")]
    public async Task<ActionResult<AuthorizationUrlResponse>> Login(
        [FromQuery] string? redirectUri,
        [FromQuery] string[]? scopes)
    {
        var baseRedirectUri = redirectUri ?? _oIdcAuthSettings.RedirectUri;
        var result = await _authService.GetAuthorizationUrlAsync(baseRedirectUri, scopes);

        Response.Cookies.Append("auth_session", result.SessionId,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(10)
            });

        return Ok(result);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] AuthorizationCodeRequest request)
    {
        var sessionId = Request.Cookies["auth_session"];

        var clientContext = await _authService.HandleCallbackAsync(
            new AuthorizationCodeRequest(request.Code, request.State, sessionId));

        Response.Cookies.Append(ClientIdCookieName, clientContext.ClientId,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

        Response.Cookies.Delete("auth_session");

        return Ok(new { message = "Authentication successful", clientId = clientContext.ClientId });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var clientId = Request.Cookies[ClientIdCookieName];

        if (!string.IsNullOrEmpty(clientId))
        {
            await _authService.LogoutAsync(clientId);
        }

        Response.Cookies.Delete(ClientIdCookieName);

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var clientId = Request.Cookies[ClientIdCookieName];

        if (string.IsNullOrEmpty(clientId))
            return Unauthorized(new { message = "Not authenticated" });

        var context = await _authService.GetClientContextAsync(clientId);

        if (context == null)
            return Unauthorized(new { message = "Session expired" });

        return Ok(new { authenticated = true, clientId = context.ClientId, expiresAt = context.ExpiresAt });
    }
}
