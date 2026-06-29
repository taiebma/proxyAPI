namespace ProxyAPI.Presentation.Extensions;

using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Domain;
using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.Cache;
using ProxyAPI.Infrastructure.Configuration;
using ProxyAPI.Infrastructure.OAuth;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        OIdcAuthSettings oIdcAuthSettings = configuration.GetSection("Oidc").Get<OIdcAuthSettings>()
            ?? throw new InvalidOperationException("Oidc settings are required in appsettings.json");
        OAuthSettings? oauthSettings = configuration.GetSection("OAuth").Get<OAuthSettings>();

        var cacheSettings = configuration.GetSection("Cache").Get<CacheSettings>()
            ?? new CacheSettings();

        services.AddSingleton(oIdcAuthSettings);
        if (oauthSettings != null)
        {
            services.AddSingleton(oauthSettings);
        }
        services.AddSingleton(cacheSettings);
        services.AddSingleton<ISessionStorage, SessionStorage>();

        services.AddSingleton<ITokenCache>(sp =>
            new MemoryTokenCache(cacheSettings.DefaultAbsoluteExpirationMinutes));

        services.AddHttpClient<IOAuthClient, OidcClient>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ISessionManager, SessionManager>();

        return services;
    }
}
