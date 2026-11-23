using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class AiRealJobStateTests
{
    private static ProcessingJob Job(string mode = "REAL") => ProcessingJob.CreateQueued(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), "{}", mode);
    [Fact]
    public void Real_failure_is_visible_and_retryable_only_by_explicit_retry()
    {
        var job = Job(); var at = DateTimeOffset.UtcNow;
        job.ApplyRealWorkerState(ProcessingJobStatus.Running, at);
        job.ApplyRealWorkerState(ProcessingJobStatus.RetryableFailure, at.AddSeconds(2), "WORKER_RESTARTED");
        Assert.Equal(ProcessingJobStatus.RetryableFailure, job.Status);
        Assert.Throws<InvalidOperationException>(() => job.ApplyRealWorkerState(ProcessingJobStatus.Running, at.AddSeconds(3)));
        job.Retry(); Assert.Equal(ProcessingJobStatus.Queued, job.Status);
    }
    [Fact]
    public void Mock_and_completed_jobs_cannot_use_real_transport_mutation()
    {
        Assert.Throws<InvalidOperationException>(() => Job("MOCK").ApplyRealWorkerState(ProcessingJobStatus.Running, DateTimeOffset.UtcNow));
        var job = Job(); job.Complete(DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => job.ApplyRealWorkerState(ProcessingJobStatus.DataFailure, DateTimeOffset.UtcNow, "BAD_INPUT"));
    }
    [Fact]
    public void Failure_requires_safe_code_and_monotonic_time()
    {
        var job = Job(); var at = DateTimeOffset.UtcNow;
        job.ApplyRealWorkerState(ProcessingJobStatus.Running, at);
        Assert.Throws<ArgumentException>(() => job.ApplyRealWorkerState(ProcessingJobStatus.DataFailure, at.AddSeconds(-1), "BAD_INPUT"));
        Assert.Throws<ArgumentException>(() => job.ApplyRealWorkerState(ProcessingJobStatus.DataFailure, at, "secret value"));
        Assert.Throws<ArgumentException>(() => job.ApplyRealWorkerState(ProcessingJobStatus.DataFailure, at));
        job.ApplyRealWorkerState(ProcessingJobStatus.Cancelled, at);
        Assert.Equal(ProcessingJobStatus.Cancelled, job.Status);
    }
}
