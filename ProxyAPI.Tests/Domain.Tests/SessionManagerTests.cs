using FluentAssertions;
using Moq;
using ProxyAPI.Domain;
using ProxyAPI.Domain.Entities;
using ProxyAPI.Infrastructure.Interfaces;
using Xunit;

namespace ProxyAPI.Tests.Domain.Tests;

public class SessionManagerTests
{


    private readonly Mock<ISessionStorage> _mockSessionStorage;
    private readonly SessionManager _sessionManager;

    public SessionManagerTests()
    {
        _mockSessionStorage = new Mock<ISessionStorage>();
        _sessionManager = new SessionManager(_mockSessionStorage.Object);
    }
    
    [Fact]
    public void AddSession_WithValidSession_Successfully()
    {
        AuthenticationSession authenticationSession = new AuthenticationSession("test-state");

        _sessionManager.AddSession(authenticationSession);
    }

    [Fact]
    public void AddSession_WithInValidSession_ThrowArgumentNullException()
    {
        AuthenticationSession authenticationSession = null!;

        var act = () =>_sessionManager.AddSession(authenticationSession);
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'AuthenticationSession')");
    }

    [Fact]
    public void GetSession_WithEmptyState_ReturnNull()
    {
        string state = string.Empty;

        var res = _sessionManager.GetSession(state);

        res.Should().BeNull();
    }

    [Fact]
    public void GetSession_WithValidStateNotFound_ReturnNull()
    {
        string state = "state-1";
        _mockSessionStorage.Setup(x => x.GetSession(state)).Returns((AuthenticationSession)null);

        var res = _sessionManager.GetSession(state);

        res.Should().BeNull();
    }

    [Fact]
    public void GetSession_WithValidState_ReturnsSession()
    {
        var state = "valid-state";
        _mockSessionStorage.Setup(x => x.GetSession(state)).Returns(new AuthenticationSession(state));

        var session = _sessionManager.GetSession(state);

        session.Should().NotBeNull();
        session.IsExpired.Should().BeFalse();
        session.State.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetSession_WithValidStateExpired_ReturnNull()
    {
        var state = "valid-state";
        _mockSessionStorage.Setup(x => x.GetSession(state)).Returns(new AuthenticationSession(state, null, 0));

        var session = _sessionManager.GetSession(state);

        session.Should().BeNull();
    }

    [Fact]
    public void RemoveSession_WithValidSessionId_Successfully()
    {
        AuthenticationSession authenticationSession = new AuthenticationSession("test-state");

        _sessionManager.RemoveSession("session-id");
    }

}