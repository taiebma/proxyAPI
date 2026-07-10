using Microsoft.Extensions.Logging;

namespace ProxyAPI.Presentation.Extensions.Logging;

public sealed class ProxyAPILogger(
    string name,
    Func<ProxyAPILoggerConfiguration> getCurrentConfig) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        ProxyAPILoggerConfiguration config = getCurrentConfig();
        if (config.EventId == 0 || config.EventId == eventId.Id)
        {
            Console.Write($"{config.AppLogConfiguration.ApplicationName}:{config.AppLogConfiguration.ModuleName}:[{eventId.Id,2}: {logLevel,-12}]");
            Console.Write($" : {name} - ");
            Console.Write($"{formatter(state, exception)}");            
            Console.WriteLine();
        }
    }
}