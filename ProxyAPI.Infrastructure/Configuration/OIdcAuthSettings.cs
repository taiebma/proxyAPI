namespace ProxyAPI.Infrastructure.Configuration;

public class OIdcAuthSettings
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:5000/auth/callback";
    public string[]? Scopes { get; set; } = new[] { "openid", "profile", "offline_access" };
    public string HeaderName { get; set; } = string.Empty;
}