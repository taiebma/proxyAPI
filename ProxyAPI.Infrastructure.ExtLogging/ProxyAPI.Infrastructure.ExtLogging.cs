
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.ExtLogging;

public class ProxyAPIInfrastructureExtLogging : IProxyAPIExtension
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register the middleware cache service
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, MidwLoggerProvider>());
        services.AddSingleton<IPluginLoggerMarker, MidwLoggerProvider>();
        LoggerProviderOptions.RegisterProviderOptions
            <MidwLoggerConfiguration, MidwLoggerProvider>(services);
        services.Configure<MidwLoggerConfiguration>(configuration.GetSection("Logging:MidwLog"));

    }

}