using Microsoft.Extensions.Logging;

namespace ProxyAPI.Presentation.Extensions.Logging;

public sealed class ProxyAPILoggerConfiguration
{
    public int EventId { get; set; }

    public LogConfiguration AppLogConfiguration { get; set; } = new()
    {
        ApplicationName = string.Empty,
        ModuleName = string.Empty
    };

    public sealed class LogConfiguration
    {
        public string ApplicationName { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
    }
}