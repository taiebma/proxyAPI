using Microsoft.Extensions.Logging;

namespace ProxyAPI.Infrastructure.ExtLogging;

public sealed class MidwLogger(
    string name,
    Func<MidwLoggerConfiguration> getCurrentConfig) : ILogger
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

        MidwLoggerConfiguration config = getCurrentConfig();
        if (config.EventId == 0 || config.EventId == eventId.Id)
        {
            Console.Write($"{config.AppLogConfiguration.ApplicationName}:{config.AppLogConfiguration.ModuleName}:[{eventId.Id,2}: {logLevel,-12}]");
            Console.Write($" : {name} - ");
            Console.Write($"{formatter(state, exception)}");            
            Console.WriteLine();
        }
    }
}