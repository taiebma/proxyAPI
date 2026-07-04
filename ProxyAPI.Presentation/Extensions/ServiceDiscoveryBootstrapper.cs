using System.Reflection;
using Microsoft.Extensions.ServiceDiscovery;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Presentation.Extensions;

public static class ServiceDiscoveryBootstrapper
{
    private const string PluginAssemblyFileName = "ProxyAPI.Infrastructure.Ext*.dll";

    public static void TryLoadPluginExtension(IServiceCollection services, IConfiguration configuration, ILogger logger)
    {

        string[] pluginFiles = Directory.GetFiles(AppContext.BaseDirectory, PluginAssemblyFileName);
        if (pluginFiles.Count() == 0)
        {
            return ; // DLL absente : comportement attendu, pas une erreur
        }

        try
        {
            foreach(var pluginPath in pluginFiles)
            {
                var assembly = Assembly.LoadFrom(pluginPath);

                var implementationType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IProxyAPIExtension).IsAssignableFrom(t)
                                            && !t.IsAbstract
                                            && !t.IsInterface);

                if (implementationType is null)
                {
                    logger.LogWarning(
                        "{File} ne contient aucune implémentation de IProxyAPIExtension.",
                        assembly.ManifestModule.Name);
                    continue;
                }
                if (configuration.GetValue<bool>(assembly.ManifestModule.Name) == true)
                {
                    var extension = (IProxyAPIExtension)Activator.CreateInstance(implementationType);
                    extension!.ConfigureServices(services, configuration);
                    logger.LogInformation(
                        "Plugin {File} Charged.",
                        assembly.ManifestModule.Name);

                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec du chargement du plugin {File}.", PluginAssemblyFileName);
        }
    }
}
