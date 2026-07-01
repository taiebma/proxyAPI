namespace ProxyAPI.Presentation.Extensions;

using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Domain;
using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.Cache;
using ProxyAPI.Infrastructure.Configuration;
using ProxyAPI.Infrastructure.OAuth;
using ProxyAPI.Infrastructure.Audit;
using ProxyAPI.Domain.Audit;

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
            services.AddHttpClient<IOAuthClient, OAuthClient>()
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                });
            services.AddScoped<ITokenService, TokenService>();
        }
        services.AddSingleton(cacheSettings);
        services.AddSingleton<ISessionStorage, SessionStorage>();

        services.AddSingleton<ITokenCache>(sp =>
            new MemoryTokenCache(cacheSettings.DefaultAbsoluteExpirationMinutes));

        services.AddHttpClient<IOidcClient, OidcClient>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });


        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ISessionManager, SessionManager>();

        services.AddSingleton<AuditBackground>();
        services.AddSingleton<IGlobalAudit<AuditEntity>>(sp => 
            sp.GetRequiredService<AuditBackground>());
        services.AddHostedService(sp => 
            sp.GetRequiredService<AuditBackground>());
        services.AddSingleton<IAuditService, AuditConsole>();
        return services;
    }
}
