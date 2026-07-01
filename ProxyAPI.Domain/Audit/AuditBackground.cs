using System.Threading.Channels;
using ProxyAPI.Infrastructure.Audit;
using ProxyAPI.Infrastructure.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProxyAPI.Domain.Audit;

public class AuditBackground: BackgroundService, IGlobalAudit<AuditEntity>
{
    private readonly Channel<AuditEntity> _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IAuditService> _auditServices;
    private readonly ILogger<AuditBackground> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Un seul batch à la fois
    
    public AuditBackground(
        IServiceProvider serviceProvider,
        ILogger<AuditBackground> logger,
        IEnumerable<IAuditService> auditServices)
    {
        // Crée un channel unbounded (illimité)
        _queue = Channel.CreateUnbounded<AuditEntity>(
            new UnboundedChannelOptions
            {
                SingleReader = true, // Optimisation : un seul consumer
                SingleWriter = false // Plusieurs producers possibles
            }
        );
        
        _serviceProvider = serviceProvider;
        _auditServices = auditServices;
        _logger = logger;
    }

    public async Task LogRequest(AuditEntity audit)
    {
        await _queue.Writer.WriteAsync(audit);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("AuditLogger started");

        while (!ct.IsCancellationRequested)
        {
           // Attend qu'au moins 1 item soit disponible
            var audit = await _queue.Reader.ReadAsync(ct);

            foreach (var auditService in _auditServices)
            {
                try
                {
                    auditService.LogRequest(audit.Timestamp, audit.UserId, audit.Method, audit.Uri, audit.StatusCode, audit.Body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging request to {AuditService}", auditService.GetType().Name);
                }
            }

            await Task.Delay(100, ct);
        }
    }

    public override void Dispose()
    {
        _semaphore?.Dispose();
        base.Dispose();
    }
}