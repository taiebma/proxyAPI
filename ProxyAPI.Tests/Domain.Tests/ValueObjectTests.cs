namespace ProxyAPI.Tests.Domain.Tests;

using FluentAssertions;
using ProxyAPI.Infrastructure.ValueObjects;
using Xunit;

public class ClientIdTests
{
    [Fact]
    public void ClientId_WithValidValue_CreatesSuccessfully()
    {
        var value = "client-123";
        var clientId = new ClientId(value);

        clientId.Value.Should().Be(value);
    }

    [Fact]
    public void ClientId_WithEmptyValue_ThrowsArgumentException()
    {
        var act = () => new ClientId(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ClientId_WithNullValue_ThrowsArgumentException()
    {
        var act = () => new ClientId(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ClientId_EqualityWorks()
    {
        var id1 = new ClientId("same-id");
        var id2 = new ClientId("same-id");

        id1.Should().Be(id2);
    }
}

public class TokenValueTests
{
    [Fact]
    public void TokenValue_WithValidData_CreatesSuccessfully()
    {
        var accessToken = "access-token";
        var refreshToken = "refresh-token";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var token = new TokenValue(accessToken, refreshToken, expiresAt);

        token.AccessToken.Should().Be(accessToken);
        token.RefreshToken.Should().Be(refreshToken);
        token.ExpiresAt.Should().Be(expiresAt);
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void TokenValue_WhenExpired_IsExpiredReturnsTrue()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(-1);
        var token = new TokenValue("token", null, DateTime.UtcNow.AddHours(1));

        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void TokenValue_WithEmptyAccessToken_ThrowsArgumentException()
    {
        var act = () => new TokenValue(string.Empty, null, DateTime.UtcNow.AddHours(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TokenValue_WithPastExpiryTime_ThrowsArgumentException()
    {
        var act = () => new TokenValue("token", null, DateTime.UtcNow.AddMinutes(-1));
        act.Should().Throw<ArgumentException>();
    }
}
