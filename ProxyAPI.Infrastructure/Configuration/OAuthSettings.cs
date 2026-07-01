namespace ProxyAPI.Infrastructure.Configuration;

public class OAuthSettings
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string[]? Scopes { get; set; } = null;
    public string HeaderName { get; set; } = string.Empty;
    public Dictionary<string, string>? AdditionalParameters { get; set; }
}
