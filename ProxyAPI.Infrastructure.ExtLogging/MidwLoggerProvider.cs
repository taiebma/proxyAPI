using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyAPI.Infrastructure.Interfaces;

namespace ProxyAPI.Infrastructure.ExtLogging;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("ProxyConsole")]
public sealed class MidwLoggerProvider : ILoggerProvider, IPluginLoggerMarker
{
    private readonly IDisposable? _onChangeToken;
    private MidwLoggerConfiguration _currentConfig;
    private readonly ConcurrentDictionary<string, MidwLogger> _loggers =
        new(StringComparer.OrdinalIgnoreCase);

    public MidwLoggerProvider(
        IOptionsMonitor<MidwLoggerConfiguration> config)
    {
        _currentConfig = config.CurrentValue;
        _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new MidwLogger(name, GetCurrentConfig));

    private MidwLoggerConfiguration GetCurrentConfig() => _currentConfig;

    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }
}