namespace ProxyAPI.Infrastructure.Interfaces;

public interface IAuthenticationSession
{
    public string State { get; set; }
    public string? CodeVerifier { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; }

    public bool ValidateState(string state);
}