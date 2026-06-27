namespace ProxyAPI.Tests.Infrastructure.Tests;

using FluentAssertions;
using ProxyAPI.Domain.ValueObjects;
using ProxyAPI.Infrastructure.Cache;
using Xunit;

public class MemoryTokenCacheTests
{
    [Fact]
    public void Set_WithValidToken_StoresToken()
    {
        var cache = new MemoryTokenCache();
        var clientId = new ClientId("client-1");
        var token = new TokenValue("access-token", null, DateTime.UtcNow.AddHours(1));

        cache.Set(clientId, token);

        var retrieved = cache.Get(clientId);
        retrieved.Should().NotBeNull();
        retrieved!.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public void Get_WithNonExistentClientId_ReturnsNull()
    {
        var cache = new MemoryTokenCache();
        var clientId = new ClientId("non-existent");

        var result = cache.Get(clientId);

        result.Should().BeNull();
    }

    [Fact]
    public void Exists_WithStoredToken_ReturnsTrue()
    {
        var cache = new MemoryTokenCache();
        var clientId = new ClientId("client-1");
        var token = new TokenValue("access-token", null, DateTime.UtcNow.AddHours(1));

        cache.Set(clientId, token);

        cache.Exists(clientId).Should().BeTrue();
    }

    [Fact]
    public void Exists_WithNonExistentClientId_ReturnsFalse()
    {
        var cache = new MemoryTokenCache();
        cache.Exists(new ClientId("non-existent")).Should().BeFalse();
    }

    [Fact]
    public void Remove_WithStoredClientId_DeletesToken()
    {
        var cache = new MemoryTokenCache();
        var clientId = new ClientId("client-1");
        var token = new TokenValue("access-token", null, DateTime.UtcNow.AddHours(1));

        cache.Set(clientId, token);
        cache.Remove(clientId);

        cache.Exists(clientId).Should().BeFalse();
    }

    [Fact]
    public void Get_WithExpiredToken_ReturnsNullAndRemovesEntry()
    {
        var cache = new MemoryTokenCache(defaultExpirationMinutes: 0);
        var clientId = new ClientId("client-1");
        var token = new TokenValue("access-token", null, DateTime.UtcNow.AddHours(1));

        cache.Set(clientId, token);
        System.Threading.Thread.Sleep(100);

        var result = cache.Get(clientId);

        result.Should().BeNull();
        cache.Exists(clientId).Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllTokens()
    {
        var cache = new MemoryTokenCache();
        var clientId1 = new ClientId("client-1");
        var clientId2 = new ClientId("client-2");
        var token = new TokenValue("access-token", null, DateTime.UtcNow.AddHours(1));

        cache.Set(clientId1, token);
        cache.Set(clientId2, token);

        cache.Clear();

        cache.Exists(clientId1).Should().BeFalse();
        cache.Exists(clientId2).Should().BeFalse();
    }

    [Fact]
    public void Set_WithNullClientId_ThrowsArgumentNullException()
    {
        var cache = new MemoryTokenCache();
        var token = new TokenValue("access-token", null, DateTime.UtcNow.AddHours(1));

        var act = () => cache.Set(null!, token);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Set_WithNullToken_ThrowsArgumentNullException()
    {
        var cache = new MemoryTokenCache();
        var clientId = new ClientId("client-1");

        var act = () => cache.Set(clientId, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
