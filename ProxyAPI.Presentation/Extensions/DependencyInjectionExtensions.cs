namespace ProxyAPI.Presentation.Extensions;

using ProxyAPI.Domain.Interfaces;
using ProxyAPI.Domain;
using ProxyAPI.Infrastructure.Interfaces;
using ProxyAPI.Infrastructure.Cache;
using ProxyAPI.Infrastructure.Configuration;
using ProxyAPI.Infrastructure.OAuth;
using ProxyAPI.Infrastructure.Audit;
using ProxyAPI.Domain.Audit;
using Microsoft.Extensions.ServiceDiscovery;
using System.Net;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        // OIDC/OAuth configuration
        OIdcAuthSettings oIdcAuthSettings = configuration.GetSection("Oidc").Get<OIdcAuthSettings>()
            ?? throw new InvalidOperationException("Oidc settings are required in appsettings.json");
        OAuthSettings? oauthSettings = configuration.GetSection("OAuth").Get<OAuthSettings>();

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

        // ServiceDicovery configuration
        services.AddServiceDiscovery();
        services.ConfigureHttpClientDefaults(static http =>
        {
            http.AddServiceDiscovery();
        });
        services.Configure<ServiceDiscoveryOptions>(options =>
        {
            options.AllowAllSchemes = false;
            options.AllowedSchemes = ["https"];
        });
        services.Configure<ConfigurationServiceEndpointProviderOptions>(static options =>
        {
            options.SectionName = "ServiceDiscovery";
        });

        // Cache configuration
        var cacheSettings = configuration.GetSection("Cache").Get<CacheSettings>()
            ?? new CacheSettings();

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

        // Audit configuration
        services.AddSingleton<AuditBackground>();
        services.AddSingleton<IGlobalAudit<AuditEntity>>(sp => 
            sp.GetRequiredService<AuditBackground>());
        services.AddHostedService(sp => 
            sp.GetRequiredService<AuditBackground>());
        services.AddSingleton<IAuditService, AuditConsole>();

        return services;
    }
}
