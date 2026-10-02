using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Processing;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class Anh02ProcessingFailureTests
{
    private static ProcessingJob Job(string mode = "MOCK") => ProcessingJob.CreateQueued(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), "{}", mode);
    [Fact]
    public void Terminal_data_failure_records_end_and_code_and_cannot_retry()
    {
        var job = Job(); var at = DateTimeOffset.Parse("2026-10-03T08:00:00Z");
        job.FailData(at, " source_not_ready ");
        Assert.Equal(ProcessingJobStatus.DataFailure, job.Status); Assert.Equal(at, job.CompletedAt);
        Assert.Equal(at, job.StartedAt); Assert.Equal("source_not_ready", job.ErrorCode); Assert.Null(job.ErrorMessage);
        Assert.Throws<InvalidOperationException>(() => job.Retry());
        Assert.Throws<InvalidOperationException>(() => job.Complete(at));
        Assert.Throws<InvalidOperationException>(() => job.FailData(at, "ai_contract_invalid"));
    }
    [Fact]
    public void Terminal_failure_does_not_reopen_successful_analysis()
    {
        var job = Job(); var at = DateTimeOffset.UtcNow; job.Complete(at);
        Assert.Throws<InvalidOperationException>(() => job.FailData(at, "candidate_stale"));
        Assert.Equal(ProcessingJobStatus.Completed, job.Status); Assert.Null(job.ErrorCode);
    }
    [Fact]
    public void Real_provider_job_cannot_use_mock_failure_transition()
    {
        var job = Job("REAL");
        Assert.Throws<InvalidOperationException>(() => job.FailData(DateTimeOffset.UtcNow, "source_not_ready"));
        Assert.Equal(ProcessingJobStatus.Queued, job.Status); Assert.Null(job.CompletedAt);
    }
    [Theory] [InlineData("")] [InlineData(" ")] [InlineData(null)]
    public void Invalid_failure_code_leaves_pending_job_unchanged(string? code)
    {
        var job = Job(); Assert.Throws<ArgumentException>(() => job.FailData(DateTimeOffset.UtcNow, code!));
        Assert.Equal(ProcessingJobStatus.Queued, job.Status); Assert.Null(job.CompletedAt);
    }
}
