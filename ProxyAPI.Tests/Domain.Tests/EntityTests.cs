namespace ProxyAPI.Tests.Domain.Tests;

using FluentAssertions;
using ProxyAPI.Domain.Entities;
using ProxyAPI.Domain.ValueObjects;
using Xunit;

public class ClientTests
{
    [Fact]
    public void Client_WithValidData_CreatesSuccessfully()
    {
        var clientId = new ClientId("client-1");
        var token = new TokenValue("access-token", null, DateTime.UtcNow.AddHours(1));

        var client = new Client(clientId, token);

        client.ClientId.Should().Be(clientId);
        client.Token.Should().Be(token);
        client.IsTokenValid.Should().BeTrue();
    }

    [Fact]
    public void Client_UpdateToken_ReplacesPreviousToken()
    {
        var clientId = new ClientId("client-1");
        var oldToken = new TokenValue("old-token", null, DateTime.UtcNow.AddHours(1));
        var newToken = new TokenValue("new-token", null, DateTime.UtcNow.AddHours(2));

        var client = new Client(clientId, oldToken);
        client.UpdateToken(newToken);

        client.Token.Should().Be(newToken);
    }

    [Fact]
    public void Client_WithNullClientId_ThrowsArgumentNullException()
    {
        var token = new TokenValue("token", null, DateTime.UtcNow.AddHours(1));
        var act = () => new Client(null!, token);

        act.Should().Throw<ArgumentNullException>();
    }
}

public class AuthenticationSessionTests
{
    [Fact]
    public void AuthenticationSession_WithValidState_CreatesSuccessfully()
    {
        var state = "valid-state-123";
        var session = new AuthenticationSession(state);

        session.State.Should().Be(state);
        session.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void AuthenticationSession_ValidateState_ReturnsTrueForMatchingState()
    {
        var state = "valid-state";
        var session = new AuthenticationSession(state);

        session.ValidateState(state).Should().BeTrue();
    }

    [Fact]
    public void AuthenticationSession_ValidateState_ReturnsFalseForDifferentState()
    {
        var session = new AuthenticationSession("original-state");
        session.ValidateState("different-state").Should().BeFalse();
    }

    [Fact]
    public void AuthenticationSession_WhenExpired_IsExpiredReturnsTrue()
    {
        var session = new AuthenticationSession("state", null, expirationMinutes: 0);
        System.Threading.Thread.Sleep(100);

        session.IsExpired.Should().BeTrue();
    }
}
