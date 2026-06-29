namespace ProxyAPI.Domain.DTOs;

public record AuthorizationCodeRequest(string Code, string State, string? SessionId);

public record TokenResponse(
    string AccessToken,
    string? RefreshToken,
    long ExpiresIn,
    string TokenType = "Bearer");

public record ClientContext(
    string ClientId,
    string AccessToken,
    string? RefreshToken,
    DateTime ExpiresAt);

public record AuthorizationUrlResponse(
    string Url,
    string State,
    string SessionId);
