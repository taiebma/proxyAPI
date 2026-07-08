using System.Net.Http.Json;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.ExtAuthz;

public class AuthzRoleProvider : IRoleProvider
{
    private readonly HttpClient _httpClient;
    private readonly ICacheService<string[]> _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AuthzRoleProvider(HttpClient httpClient, ICacheService<string[]> cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default)
    {
        var roles = _cache.Get(userId);
        if (roles == null)
        {

            try
            {                
                var response = await _httpClient.GetAsync($"/users/{Uri.EscapeDataString(userId)}/roles", ct);
                if (!response.IsSuccessStatusCode)
                    return (IReadOnlyList<string>)Array.Empty<string>();

                roles = await response.Content.ReadFromJsonAsync<string[]>(cancellationToken: ct);
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error fetching roles for user {userId}: {ex.Message}");
            }
        }
        return (IReadOnlyList<string>)(roles ?? Array.Empty<string>());
    }
}