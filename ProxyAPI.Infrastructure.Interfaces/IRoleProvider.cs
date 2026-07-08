namespace ProxyAPI.Infrastructure.Interfaces;

public interface IRoleProvider
{
    Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default);
}