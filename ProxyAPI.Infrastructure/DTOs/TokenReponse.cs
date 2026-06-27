public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public long ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";

    // Alias pour différents formats OIDC
    public string? access_token
    {
        get => AccessToken;
        set => AccessToken = value ?? string.Empty;
    }

    public string? refresh_token
    {
        get => RefreshToken;
        set => RefreshToken = value;
    }

    public long expires_in
    {
        get => ExpiresIn;
        set => ExpiresIn = value;
    }

    public string? token_type
    {
        get => TokenType;
        set => TokenType = value ?? "Bearer";
    }
}