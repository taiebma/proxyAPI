namespace ProxyAPI.Infrastructure.ValueObjects;

public class TokenValue : IEquatable<TokenValue>
{
    public string AccessToken { get; }
    public string? RefreshToken { get; }
    public DateTime ExpiresAt { get; }

    public string UserId { get; set; } = string.Empty;

    public TokenValue(string accessToken, string? refreshToken, DateTime expiresAt, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("AccessToken cannot be empty.", nameof(accessToken));

        if (expiresAt < DateTime.UtcNow)
            throw new ArgumentException("ExpiresAt must be in the future.", nameof(expiresAt));

        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        UserId = userId ?? string.Empty;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool Equals(TokenValue? other) =>
        other?.AccessToken == AccessToken && other?.ExpiresAt == ExpiresAt;

    public override bool Equals(object? obj) => obj is TokenValue tv && Equals(tv);
    public override int GetHashCode() => HashCode.Combine(AccessToken, ExpiresAt);
}
