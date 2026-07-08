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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using ProxyAPI.Presentation.Extensions.Authentication;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        // Loading Extensions
        using var bootstrapLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var bootstrapLogger = bootstrapLoggerFactory.CreateLogger("ServiceDiscoveryBootstrap");
        ServiceDiscoveryBootstrapper.TryLoadPluginExtension(services, configuration, bootstrapLogger);

        // Authentication Handler
        services.AddAuthentication("ProxyId")
            .AddScheme<AuthenticationSchemeOptions, ProxyAPIAuthenticationHandler>("ProxyId", options => { });

        // Role Transformation
        services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
        services.Configure<RoleProviderOptions>(configuration.GetSection("RoleProvider"));
        services.TryAddScoped<IRoleProvider, ConfigurationRoleProvider>();

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
            // Business OAuth service
            services.AddScoped<ITokenService, TokenService>();
        }

        // Infra OIDC
        services.AddHttpClient<IOidcClient, OidcClient>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });


        // ServiceDicovery configuration par defaut
        services.AddServiceDiscoveryCore();
        services.AddConfigurationServiceEndpointProvider();
        services.AddPassThroughServiceEndpointProvider();

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
        services.TryAddSingleton(typeof(ICacheService<>), typeof(MemoryCacheService<>));

        // Business services
        services.AddScoped<IProxyAPIAuthenticationService, ProxyAPIAuthenticationService>();
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
