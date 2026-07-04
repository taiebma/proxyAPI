namespace ProxyAPI.Tests.Infrastructure.Tests;

using FluentAssertions;
using ProxyAPI.Domain.Entities;
using ProxyAPI.Infrastructure.Interfaces;
using Xunit;

public class SessionStorageTests
{
    [Fact]
    public void AddSession_WithValidSession_StoresSession()
    {
        var sessionStorage = new SessionStorage();
        var session = new AuthenticationSession("test-state");

        sessionStorage.AddSession(session);

        var result = sessionStorage.GetSession("test-state");

        result.Should().NotBeNull();
        result!.State.Should().Be(session.State);
        result.State.Should().Be(session.State);
    }

    [Fact]
    public void AddSession_WithNullSession_ThrowsArgumentNullException()
    {
        var sessionStorage = new SessionStorage();

        var act = () => sessionStorage.AddSession(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetSession_WithUnknownState_ReturnsNull()
    {
        var sessionStorage = new SessionStorage();
        var session = new AuthenticationSession("known-state");

        sessionStorage.AddSession(session);

        var result = sessionStorage.GetSession("unknown-state");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSession_WithNullOrWhiteSpaceState_ReturnsNull(string? state)
    {
        var sessionStorage = new SessionStorage();

        var result = sessionStorage.GetSession(state!);

        result.Should().BeNull();
    }

    [Fact]
    public void RemoveSession_WithExistingSessionId_RemovesSession()
    {
        var sessionStorage = new SessionStorage();
        var session = new AuthenticationSession("delete-state");

        sessionStorage.AddSession(session);
        sessionStorage.RemoveSession(session.State);

        var result = sessionStorage.GetSession("delete-state");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveSession_WithNullOrWhiteSpaceSessionId_DoesNotThrow(string? sessionId)
    {
        var sessionStorage = new SessionStorage();

        var act = () => sessionStorage.RemoveSession(sessionId!);

        act.Should().NotThrow();
    }
}
