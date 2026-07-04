using System.Reflection;
using Microsoft.Extensions.ServiceDiscovery;
using ProxyAPI.Infrastructure.SdExtension;

namespace ProxyAPI.Presentation.Extensions;

public static class ServiceDiscoveryBootstrapper
{
    private const string PluginAssemblyFileName = "ProxyAPI.Infrastructure.SdCoac.dll";

    public static IServiceCollection AddServiceDiscoveryWithOptionalPlugin(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        services.AddServiceDiscoveryCore();

        var useCoac = configuration.GetValue<bool>("CoacSd");

        if (useCoac && TryLoadPluginExtension(logger, out var extension))
        {
            logger.LogInformation("Service discovery : plugin CoacSd chargé et activé.");
            extension!.ConfigureServices(services, configuration);
            return services;
        }

        if (useCoac)
        {
            logger.LogWarning(
                "CoacSd=true mais {File} est introuvable dans {Dir}. Bascule sur le service discovery standard.",
                PluginAssemblyFileName, AppContext.BaseDirectory);
        }

        // Fallback standard : config-based
        services.AddConfigurationServiceEndpointProvider();
        services.AddPassThroughServiceEndpointProvider();
        return services;
    }

    private static bool TryLoadPluginExtension(ILogger logger, out IServiceDiscoveryExtension? extension)
    {
        extension = null;
        var pluginPath = Path.Combine(AppContext.BaseDirectory, PluginAssemblyFileName);

        if (!File.Exists(pluginPath))
        {
            return false; // DLL absente : comportement attendu, pas une erreur
        }

        try
        {
            var assembly = Assembly.LoadFrom(pluginPath);

            var implementationType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IServiceDiscoveryExtension).IsAssignableFrom(t)
                                      && !t.IsAbstract
                                      && !t.IsInterface);

            if (implementationType is null)
            {
                logger.LogWarning(
                    "{File} ne contient aucune implémentation de IServiceDiscoveryExtension.",
                    PluginAssemblyFileName);
                return false;
            }

            extension = (IServiceDiscoveryExtension)Activator.CreateInstance(implementationType)!;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec du chargement du plugin {File}.", PluginAssemblyFileName);
            return false;
        }
    }
}
