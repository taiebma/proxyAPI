using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProxyAPI.Presentation.Extensions.Logging;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("ProxyConsole")]
public sealed class ProxyAPILoggerProvider : ILoggerProvider
{
    private readonly IDisposable? _onChangeToken;
    private ProxyAPILoggerConfiguration _currentConfig;
    private readonly ConcurrentDictionary<string, ProxyAPILogger> _loggers =
        new(StringComparer.OrdinalIgnoreCase);

    public ProxyAPILoggerProvider(
        IOptionsMonitor<ProxyAPILoggerConfiguration> config)
    {
        _currentConfig = config.CurrentValue;
        _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new ProxyAPILogger(name, GetCurrentConfig));

    private ProxyAPILoggerConfiguration GetCurrentConfig() => _currentConfig;

    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }
}