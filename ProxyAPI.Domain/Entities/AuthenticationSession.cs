using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Domain.Entities;

public class AuthenticationSession : IAuthenticationSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string State { get; set; }
    public string? CodeVerifier { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public AuthenticationSession(string state, string? codeVerifier = null, int expirationMinutes = 10)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State cannot be empty.", nameof(state));

        State = state;
        CodeVerifier = codeVerifier;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool ValidateState(string state) => !IsExpired && State == state;
}
