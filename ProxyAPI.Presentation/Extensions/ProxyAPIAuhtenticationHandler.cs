using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Infrastructure.Configuration;
using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.ValueObjects;

namespace ProxyAPI.Presentation.Extensions;

public class ProxyAPIAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{

    private const string ClientIdCookieName = "X-ProxyAPI-ClientId";

    private readonly ICacheService<TokenValue> _tokenCache; // votre collection en cache
    private readonly IProxyAPIAuthenticationService _authService;
    private readonly OAuthSettings? _settingsOAuth;
    private readonly OIdcAuthSettings _oIdcAuthSettings;

    public ProxyAPIAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICacheService<TokenValue> tokenCache,
        IProxyAPIAuthenticationService authService,
        OIdcAuthSettings oIdcAuthSettings,
        OAuthSettings? oauthSettings
        )
        : base(options, logger, encoder)
    {
        _tokenCache = tokenCache;
        _authService = authService; 
        _oIdcAuthSettings = oIdcAuthSettings;   
        _settingsOAuth = oauthSettings;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Récupération de l'ID depuis un header custom
        if (!Request.Headers.TryGetValue(ClientIdCookieName, out var idValues))
            return AuthenticateResult.NoResult(); // pas de tentative d'auth

        var proxyId = idValues.ToString();

        JwtSecurityToken? token = await GetJwtToken(proxyId);

        if (token == null)
        {
            return AuthenticateResult.Fail("Impossible de récupérer le token");
        }

        // Reconstruire le principal à partir des claims stockées lors du login
        var identity = new ClaimsIdentity(token.Claims, Scheme.Name, nameType: "sub", roleType: "role");
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    private  async Task<JwtSecurityToken?> GetJwtToken(string clientId)
    {
        var clientContext = await _authService.GetClientContextAsync(clientId);

        if (clientContext != null)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(clientContext.AccessToken);

            return token;
        }
        else
        {
            var refreshedContext = await _authService.RefreshClientContextAsync(clientId);
            if (refreshedContext != null)
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(refreshedContext.AccessToken);
                return token;
            }
            else
            {
                return null;
            }
        }
    }

}