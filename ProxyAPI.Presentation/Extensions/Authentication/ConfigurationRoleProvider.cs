using Microsoft.Extensions.Options;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Presentation.Extensions.Authentication;

public class ConfigurationRoleProvider : IRoleProvider
{
    private readonly IReadOnlyDictionary<string, string[]> _rolesByUser;

    public ConfigurationRoleProvider(IOptions<RoleProviderOptions> options)
    {
        _rolesByUser = options.Value.Roles ?? new Dictionary<string, string[]>();
    }

    public Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default)
    {
        var roles = _rolesByUser.TryGetValue(userId, out var value)
            ? value
            : Array.Empty<string>();

        return Task.FromResult<IReadOnlyList<string>>(roles);
    }
}

public class RoleProviderOptions
{
    public Dictionary<string, string[]> Roles { get; set; } = new();
}