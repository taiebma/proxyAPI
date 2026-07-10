namespace ProxyAPI.Presentation.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Infrastructure.Audit;
using ProxyAPI.Infrastructure.Configuration;
using ProxyAPI.Infrastructure.Interfaces;

[Authorize(Roles = "Developer")]
[ApiController]
[Route("api/proxy/")]
public class ProxyController : ControllerBase
{
    private const string ClientIdCookieName = "X-ProxyAPI-ClientId";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITokenService? _tokenService;
    private readonly OAuthSettings? _settingsOAuth;
    private readonly IGlobalAudit<AuditEntity> _globalAudit;
    private readonly IProxyAPIAuthenticationService _authService;
    private readonly OIdcAuthSettings _oIdcAuthSettings;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(
        IHttpClientFactory httpClientFactory, 
        IServiceProvider serviceProvider, 
        IProxyAPIAuthenticationService authService, 
        OIdcAuthSettings oIdcAuthSettings, 
        IGlobalAudit<AuditEntity> globalAudit,
        ILogger<ProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
        _oIdcAuthSettings = oIdcAuthSettings;
        _serviceProvider = serviceProvider;
        _globalAudit = globalAudit;
        _tokenService = _serviceProvider.GetService<ITokenService>();
        if (_tokenService != null)
        {
            _settingsOAuth = _serviceProvider.GetService<OAuthSettings>();
        }
        _logger = logger;
    }

    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
    [HttpPatch]
    public async Task<IActionResult> ProxyRequest()
    {
//        if (!HttpContext.Items.TryGetValue("ClientContext", out _))
//            return Unauthorized(new { error = "Not authenticated" });

        string userId = User.Identity?.Name ?? "UnknownUser";

        if (HttpContext == null)
        {
            _logger.LogError("HttpContext is null in ProxyRequest");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "HttpContext is null" });
        }

        // Verification de l'identité de l'utilisateur
        if (HttpContext.Request.Headers.TryGetValue(ClientIdCookieName, out var clientId) && !string.IsNullOrWhiteSpace(clientId))
        {
            await InitAuthHeadersAndGetUserId(HttpContext, clientId!);
        }
        else
        {
            return Unauthorized(new { error = "Invalid or expired session" });
        }

        string uri = Request.Query["uri"].ToString();

        if (string.IsNullOrWhiteSpace(uri))
            return BadRequest(new { error = "Missing 'uri' query parameter" });

        var upstreamUrl = $"{uri}";

        if (Request.QueryString.HasValue)
        {
            upstreamUrl += BuildQueryString(Request.QueryString, upstreamUrl);
        }

        var client = _httpClientFactory.CreateClient();

        try
        {
            var method = new HttpMethod(Request.Method);
            var request = new HttpRequestMessage(method, upstreamUrl);

            if (Request.ContentLength.HasValue && Request.ContentLength > 0)
            {
                request.Content = new StreamContent(Request.Body);
                if (!string.IsNullOrEmpty(Request.ContentType))
                {
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);
                }
            }

            CopyHeaders(request, _tokenService);

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Enregistrer l'audit
            await _globalAudit.LogRequest(new AuditEntity
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Method = Request.Method,
                Uri = upstreamUrl,
                StatusCode = (int)response.StatusCode
            });

            _logger.LogInformation("Proxied request for user {UserId}: {Method} {Uri} - Status: {StatusCode}", userId, Request.Method, upstreamUrl, (int)response.StatusCode);

            var result = new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };

            return result;
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Proxy request failed", details = ex.Message });
        }
    }

    private  async Task InitAuthHeadersAndGetUserId(HttpContext httpContext, string clientId)
    {
        var clientContext = await _authService.GetClientContextAsync(clientId);

        if (clientContext != null)
        {
            httpContext.Items["ClientContext"] = clientContext;
            if (!string.IsNullOrWhiteSpace(_oIdcAuthSettings?.HeaderName))
            {
                httpContext.Request.Headers[_oIdcAuthSettings.HeaderName] = clientContext.AccessToken;
            }
            else
            {
                httpContext.Request.Headers.Authorization = $"Bearer {clientContext.AccessToken}";
            }
            return;
        }
        else
        {
            var refreshedContext = await _authService.RefreshClientContextAsync(clientId);
            if (refreshedContext != null)
            {
                httpContext.Items["ClientContext"] = refreshedContext;
                if (!string.IsNullOrWhiteSpace(_oIdcAuthSettings?.HeaderName))
                {
                    httpContext.Request.Headers[_oIdcAuthSettings.HeaderName] = refreshedContext.AccessToken;
                }
                else
                {
                    httpContext.Request.Headers.Authorization = $"Bearer {refreshedContext.AccessToken}";
                }
                return ;
            }
            else
            {
                throw new UnauthorizedAccessException("Invalid or expired session");
            }
        }
    }

    private string BuildQueryString(QueryString queryString, string upstreamUrl)
    {
        var queryItems = System.Web.HttpUtility.ParseQueryString(queryString.Value??string.Empty);
        queryItems.Remove("uri");
        
        if (queryItems.Count > 0)
        {
            string remainingQuery = queryItems.ToString()!; // Génère automatiquement la chaîne formatée k1=v1&k2=v2
            if (!string.IsNullOrEmpty(remainingQuery))
            {
                return (upstreamUrl.Contains("?") ? "&" : "?") + remainingQuery;
            }
        }
        return string.Empty;
    }
    private void CopyHeaders(HttpRequestMessage request, ITokenService? tokenService)
    {
        foreach (var header in Request.Headers)
        {
            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                !header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) &&
                !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {

                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        if (tokenService != null && _settingsOAuth != null)
        {
            string token = tokenService.GetTokenAsync().Result.AccessToken;
            if (string.IsNullOrWhiteSpace(_settingsOAuth.HeaderName))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                request.Headers.TryAddWithoutValidation(_settingsOAuth.HeaderName, token);
            }
        }
    }
}
