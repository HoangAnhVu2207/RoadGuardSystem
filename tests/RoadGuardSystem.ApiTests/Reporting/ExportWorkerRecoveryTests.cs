using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.API.Workers;
using RoadGuardSystem.Services.Exports;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reporting;

// No database/container: proves loop recovery and scope disposal, not durable SQL completion.
public sealed class ExportWorkerRecoveryTests
{
    [Fact]
    public async Task OneProcessingExceptionDoesNotPreventNextClaimAndEveryScopeIsDisposed()
    {
        var reachedIdle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = DispatchProxy.Create<IExportService, ExportProxy>();
        var proxy = (ExportProxy)(object)service;
        proxy.Process = () => proxy.Calls switch
        {
            1 => throw new InvalidOperationException("synthetic renderer failure"),
            2 => Task.FromResult(true),
            _ => Idle()
        };
        Task<bool> Idle() { reachedIdle.TrySetResult(); return Task.FromResult(false); }
        var scopes = new Scopes(service);
        using var worker = new ExportWorker(scopes, NullLogger<ExportWorker>.Instance, TimeProvider.System,
            Options.Create(new ExportOptions { WorkerPollSeconds = 1 }));
        await worker.StartAsync(default);
        try { await reachedIdle.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        finally { using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5)); await worker.StopAsync(deadline.Token); }
        Assert.True(proxy.Calls >= 3);
        Assert.Equal(scopes.Created, scopes.Disposed);
        Assert.Equal(proxy.Calls, scopes.Created);
        Assert.True(worker.ExecuteTask!.IsCompleted);
        Assert.False(worker.ExecuteTask.IsFaulted);
    }

    [Fact]
    public async Task CancellationDuringProcessingDisposesScopeAndFreshWorkerCanResume()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = DispatchProxy.Create<IExportService, ExportProxy>();
        var proxy = (ExportProxy)(object)service;
        proxy.ProcessWithToken = async token =>
        {
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return true;
        };
        var scopes = new Scopes(service);
        using (var first = new ExportWorker(scopes, NullLogger<ExportWorker>.Instance, TimeProvider.System,
            Options.Create(new ExportOptions { WorkerPollSeconds = 1 })))
        {
            await first.StartAsync(default);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await first.StopAsync(deadline.Token);
            Assert.True(first.ExecuteTask!.IsCompleted); Assert.False(first.ExecuteTask.IsFaulted);
        }
        Assert.Equal(1, scopes.Created); Assert.Equal(scopes.Created, scopes.Disposed);
        var resumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        proxy.ProcessWithToken = _ => { resumed.TrySetResult(); return Task.FromResult(false); };
        using var second = new ExportWorker(scopes, NullLogger<ExportWorker>.Instance, TimeProvider.System,
            Options.Create(new ExportOptions { WorkerPollSeconds = 1 }));
        await second.StartAsync(default);
        await resumed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await second.StopAsync(stop.Token);
        Assert.Equal(scopes.Created, scopes.Disposed);
        Assert.True(second.ExecuteTask!.IsCompleted); Assert.False(second.ExecuteTask.IsFaulted);
        // Durable lease recovery is verified separately by ExportLeaseRecoverySqlTests.
    }

    public class ExportProxy : DispatchProxy
    {
        public Func<Task<bool>> Process { get; set; } = () => Task.FromResult(false);
        public Func<CancellationToken, Task<bool>>? ProcessWithToken { get; set; }
        public int Calls { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IExportService.ProcessNextAsync)) throw new NotSupportedException();
            Calls++; return ProcessWithToken is null ? Process() : ProcessWithToken((CancellationToken)args![0]!);
        }
    }

    private sealed class Scopes(IExportService service) : IServiceScopeFactory
    {
        public int Created { get; private set; }
        public int Disposed { get; private set; }
        public IServiceScope CreateScope() { Created++; return new Scope(this, service); }
        private sealed class Scope(Scopes owner, IExportService service) : IServiceScope, IServiceProvider
        {
            public IServiceProvider ServiceProvider => this;
            public object? GetService(Type type) => type == typeof(IExportService) ? service : null;
            public void Dispose() => owner.Disposed++;
        }
    }
}
