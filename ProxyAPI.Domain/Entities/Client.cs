namespace ProxyAPI.Domain.Entities;

using ProxyAPI.Infrastructure.ValueObjects;

public class Client
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ClientId ClientId { get; set; }
    public TokenValue Token { get; set; }
    public string? SessionCookie { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Client(ClientId clientId, TokenValue token, string? sessionCookie = null)
    {
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        Token = token ?? throw new ArgumentNullException(nameof(token));
        SessionCookie = sessionCookie;
    }

    public bool IsTokenValid => !Token.IsExpired;

    public void UpdateToken(TokenValue newToken)
    {
        Token = newToken ?? throw new ArgumentNullException(nameof(newToken));
    }
}
