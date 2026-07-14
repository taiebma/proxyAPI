using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProxyAPI.Domain.Audit;
using ProxyAPI.Infrastructure.Audit;
using ProxyAPI.Infrastructure.Interfaces;
using Xunit;

namespace ProxyAPI.Tests.Domain.Tests;

public class AuditBackgroundTests
{
    [Fact]
    public async Task LogRequest_WhenAuditIsProvided_ProcessesItThroughAuditServices()
    {
        var auditService = new Mock<IAuditService>();
        var auditBackground = CreateSut([auditService.Object]);
        using var cts = new CancellationTokenSource();
        var processed = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        auditService
            .Setup(x => x.LogRequest(
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>()))
            .Callback(() => processed.TrySetResult(null));

        await auditBackground.StartAsync(cts.Token);
        await auditBackground.LogRequest(new AuditEntity
        {
            UserId = "user-1",
            Method = "GET",
            Uri = "https://example.test/resource",
            StatusCode = 200,
            Body = "body"
        });

        await processed.Task.WaitAsync(TimeSpan.FromSeconds(2));

        auditService.Verify(x => x.LogRequest(
            It.IsAny<DateTime>(),
            "user-1",
            "GET",
            "https://example.test/resource",
            200,
            "body"), Times.Once);

        await auditBackground.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOneAuditServiceThrows_ContinuesProcessingWithOtherServices()
    {
        var failingService = new Mock<IAuditService>();
        var succeedingService = new Mock<IAuditService>();
        var auditBackground = CreateSut([failingService.Object, succeedingService.Object]);
        using var cts = new CancellationTokenSource();
        var processed = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        failingService
            .Setup(x => x.LogRequest(
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>()))
            .Throws(new InvalidOperationException("boom"));

        succeedingService
            .Setup(x => x.LogRequest(
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>()))
            .Callback(() => processed.TrySetResult(null));

        await auditBackground.StartAsync(cts.Token);
        await auditBackground.LogRequest(new AuditEntity
        {
            UserId = "user-2",
            Method = "POST",
            Uri = "https://example.test/submit",
            StatusCode = 201
        });

        await processed.Task.WaitAsync(TimeSpan.FromSeconds(2));

        succeedingService.Verify(x => x.LogRequest(
            It.IsAny<DateTime>(),
            "user-2",
            "POST",
            "https://example.test/submit",
            201,
            null), Times.Once);

        await auditBackground.StopAsync(CancellationToken.None);
    }
/*
    [Fact]
    public async Task LogRequest_WhenAuditIsNull_ThrowsArgumentNullException()
    {
        var auditBackground = CreateSut([]);

        var act = async () => await auditBackground.LogRequest(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
*/
    private static AuditBackground CreateSut(IEnumerable<IAuditService> auditServices)
    {
        return new AuditBackground(
            Mock.Of<IServiceProvider>(),
            NullLogger<AuditBackground>.Instance,
            auditServices);
    }
}
