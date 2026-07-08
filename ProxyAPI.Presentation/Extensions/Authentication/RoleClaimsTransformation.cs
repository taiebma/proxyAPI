using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Presentation.Extensions.Authentication;

public class RoleClaimsTransformation : IClaimsTransformation
{
    private readonly IRoleProvider _roleService;
    private readonly ICacheService<IReadOnlyList<string>> _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public RoleClaimsTransformation(IRoleProvider roleService, ICacheService<IReadOnlyList<string>> cache)
    {
        _roleService = roleService;
        _cache = cache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not { IsAuthenticated: true } identity)
            return principal;

        var claimsIdentity = (ClaimsIdentity)identity;

        // Évite de re-transformer si déjà fait dans le même pipeline de requête
        if (claimsIdentity.HasClaim(c => c.Type == ClaimTypes.Role))
            return principal;

        var userId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return principal;

        var roles = _cache.Get(userId);
        if (roles == null)
        {
            roles = await _roleService.GetRolesAsync(userId);
            _cache.Set(userId, roles);
        }

        foreach (var role in roles ?? Array.Empty<string>())
        {
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return principal;
    }
}