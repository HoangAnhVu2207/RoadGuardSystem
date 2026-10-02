using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Retention;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Retention;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

public sealed class Anh02ReceiptAuthorizationTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Theory]
    [InlineData("ai", false)] [InlineData("ai", true)]
    [InlineData("export", false)] [InlineData("export", true)]
    [InlineData("hold", false)] [InlineData("hold", true)]
    [InlineData("evaluate", false)] [InlineData("evaluate", true)]
    public async Task Real_caller_denies_receipt_after_preflight_revocation(string caller, bool conflict)
    {
        var (actor, project) = await SeedAsync(caller == "hold");
        var key = Guid.NewGuid().ToString();
        await using (var first = sql.CreateDbContext())
        {
            if (caller == "ai")
            {
                // Receipt fixture isolates AI replay; creation/source/worker is covered by real HTTP tests.
                first.Add(IdempotencyRecord.Create(actor, project, "Anh02.AiMock.Create", key, new string('a', 64), Guid.NewGuid(), "{}", DateTimeOffset.UtcNow));
                await first.SaveChangesAsync();
            }
            else await InvokeAsync(first, caller, actor, project, key, false);
        }
        var baseline = await CountsAsync(actor, project);
        var hook = new ReceiptHook(async (_, _) => await RevokeAsync(actor, caller));
        await using var db = sql.CreateDbContext(hook);
        if (caller == "export")
        {
            var result = await ExportAsync(db, actor, project, key, conflict, (_, _, _) => throw new InvalidOperationException("Replay must not capture"));
            Assert.Equal("forbidden", result.ErrorCode); Assert.Null(result.Export);
        }
        else if (caller == "ai")
        {
            var error = await Assert.ThrowsAsync<AiRequestException>(() => InvokeAsync(db, caller, actor, project, key, conflict));
            Assert.Equal(403, error.Status); Assert.Equal("access_forbidden", error.Code);
        }
        else
        {
            var error = await Assert.ThrowsAsync<RetentionRequestException>(() => InvokeAsync(db, caller, actor, project, key, conflict));
            Assert.Equal(403, error.Status); Assert.Equal("access_forbidden", error.Code);
        }
        Assert.Equal(1, hook.Calls);
        Assert.Equal(baseline, await CountsAsync(actor, project));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Export_duplicate_key_recovery_checks_current_authority(bool revoked)
    {
        var (actor, project) = await SeedAsync(false); var key = Guid.NewGuid().ToString(); var captures = 0;
        await using var db = sql.CreateDbContext();
        var result = await ExportAsync(db, actor, project, key, false, async (id, now, token) =>
        {
            captures++; await using var competitor = sql.CreateDbContext();
            Assert.NotNull((await ExportAsync(competitor, actor, project, key, false, (sid, at, _) => Task.FromResult(Capture(sid, actor, project, at)), token)).Export);
            if (revoked) await RevokeAsync(actor, "export");
            token.ThrowIfCancellationRequested(); return Capture(id, actor, project, now);
        });
        Assert.Equal(1, captures);
        Assert.Equal(revoked ? "forbidden" : null, result.ErrorCode);
        Assert.Equal(!revoked, result.Export is not null);
        var counts = await CountsAsync(actor, project); Assert.Equal((1, 1, 1, 0, 0, 0), counts);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Export_acknowledgement_recovery_checks_current_authority(bool revoked)
    {
        var (actor, project) = await SeedAsync(false); var captures = 0;
        var ack = new FailFirstCommittedInterceptor();
        await using var db = sql.CreateDbContext(new AfterCommit(async () => { if (revoked) await RevokeAsync(actor, "export"); }), ack);
        var result = await ExportAsync(db, actor, project, Guid.NewGuid().ToString(), false, (id, at, token) =>
        { token.ThrowIfCancellationRequested(); captures++; return Task.FromResult(Capture(id, actor, project, at)); });
        Assert.Equal(1, captures); Assert.Equal(1, ack.FailureCount);
        Assert.Equal(revoked ? "forbidden" : null, result.ErrorCode); Assert.Equal(!revoked, result.Export is not null);
        Assert.Equal((1, 1, 1, 0, 0, 0), await CountsAsync(actor, project));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Real_export_guard_preserves_exception_and_cancellation(bool cancelled)
    {
        var (actor, project) = await SeedAsync(false); var key = Guid.NewGuid().ToString();
        await using (var first = sql.CreateDbContext()) await InvokeAsync(first, "export", actor, project, key, false);
        using var command = new CancellationTokenSource();
        Exception failure = cancelled ? new OperationCanceledException(command.Token) : new TestTransientException("Authoritative read failure");
        var interceptor = new AuthorityFailure(failure, command.Token);
        await using var db = sql.CreateRetryingDbContext(interceptor);
        var caught = await Assert.ThrowsAnyAsync<Exception>(() => ExportAsync(db, actor, project, key, false, (_, _, _) => throw new InvalidOperationException("No handler"), command.Token));
        Assert.Same(failure, caught); Assert.Equal(1, interceptor.Calls);
        Assert.Equal((1, 1, 1, 0, 0, 0), await CountsAsync(actor, project));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Export_denies_role_deactivation_after_receipt_preflight(bool conflict)
    {
        var (actor, project) = await SeedAsync(false); var key = Guid.NewGuid().ToString();
        await using (var first = sql.CreateDbContext()) await InvokeAsync(first, "export", actor, project, key, false);
        var hook = new ReceiptHook(async (_, token) =>
        { await using var revoke = sql.CreateDbContext(); await revoke.Database.ExecuteSqlRawAsync("UPDATE Roles SET IsActive=0 WHERE Code='PM'", token); });
        try
        {
            await using var db = sql.CreateDbContext(hook);
            var result = await ExportAsync(db, actor, project, key, conflict, (_, _, _) => throw new InvalidOperationException("No capture"));
            Assert.Equal("forbidden", result.ErrorCode); Assert.Null(result.Export); Assert.Equal(1, hook.Calls);
            Assert.Equal((1, 1, 1, 0, 0, 0), await CountsAsync(actor, project));
        }
        finally
        { await using var restore = sql.CreateDbContext(); await restore.Database.ExecuteSqlRawAsync("UPDATE Roles SET IsActive=1 WHERE Code='PM'"); }
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Export_denies_password_change_requirement_after_preflight(bool conflict)
    {
        var (actor, project) = await SeedAsync(false); var key = Guid.NewGuid().ToString();
        await using (var first = sql.CreateDbContext()) await InvokeAsync(first, "export", actor, project, key, false);
        var hook = new ReceiptHook(async (_, token) =>
        { await using var revoke = sql.CreateDbContext(); await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET MustChangePassword=1 WHERE Id={actor}", token); });
        await using var db = sql.CreateDbContext(hook);
        var result = await ExportAsync(db, actor, project, key, conflict, (_, _, _) => throw new InvalidOperationException("No capture"));
        Assert.Equal("forbidden", result.ErrorCode); Assert.Null(result.Export); Assert.Equal(1, hook.Calls);
        Assert.Equal((1, 1, 1, 0, 0, 0), await CountsAsync(actor, project));
    }

    [Fact]
    public async Task Real_export_acknowledgement_recovery_does_not_return_receipt_after_cancellation()
    {
        var (actor, project) = await SeedAsync(false); var captures = 0; using var command = new CancellationTokenSource();
        var ack = new FailFirstCommittedInterceptor();
        await using var db = sql.CreateDbContext(new AfterCommit(() => { command.Cancel(); return Task.CompletedTask; }), ack);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ExportAsync(db, actor, project, Guid.NewGuid().ToString(), false,
            (id, at, token) => { Assert.Equal(command.Token, token); captures++; return Task.FromResult(Capture(id, actor, project, at)); }, command.Token));
        // SqlClient can supply a default token in its own transaction-start cancellation exception.
        // Preserve that provider exception; guard token/exception identity has separate coverage above.
        Assert.True(command.IsCancellationRequested); Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(1, captures); Assert.Equal(1, ack.FailureCount);
        Assert.Equal((1, 1, 1, 0, 0, 0), await CountsAsync(actor, project));
    }

    private async Task<(Guid Actor, Guid Project)> SeedAsync(bool supervisor)
    {
        await using var db = sql.CreateDbContext(); await sql.SeedRolesAsync(db);
        var actor = Guid.NewGuid(); var project = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = actor, UserName = actor.ToString(), NormalizedUserName = actor.ToString().ToUpperInvariant(), DisplayName = "ANH02 correction fixture", PasswordHash = "non-login-fixture", SecurityStamp = actor.ToString(), RoleCode = supervisor ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow });
        db.Projects.Add(Project.Create(project, project.ToString(), "ANH02 correction", null, null, null, null, DateTimeOffset.UtcNow));
        if (!supervisor) db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, actor, new DateOnly(2000, 1, 1)));
        await db.SaveChangesAsync(); return (actor, project);
    }
    private async Task RevokeAsync(Guid actor, string caller)
    {
        await using var db = sql.CreateDbContext();
        if (caller == "export") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE UserId={actor}");
        else await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status=2 WHERE Id={actor}");
    }
    private static async Task InvokeAsync(RoadGuardDbContext db, string caller, Guid actor, Guid project, string key, bool conflict)
    {
        if (caller == "ai")
        {
            var repo = new Anh02AiRepository(db, new(db), new ProjectMembershipReadModel(db), TimeProvider.System);
            await repo.AdmitAsync(actor, UserRoleCode.ProjectManager, project,
                new(Guid.NewGuid(), new(Guid.NewGuid(), Guid.NewGuid(), [], "SURFACE"), Guid.NewGuid(), "fixture", "fixture", "synthetic-road-v1", "VIDEO_ANALYSIS", "fixture"),
                key, new string(conflict ? 'b' : 'a', 64), (_, _, _, _, _) => throw new InvalidOperationException("Replay must not build manifest"), default);
        }
        else if (caller == "export") await ExportAsync(db, actor, project, key, conflict, (id, at, _) => Task.FromResult(Capture(id, actor, project, at)));
        else
        {
            var repo = new RetentionRepository(db, new(db), new RetentionInventoryRepository(db, []), TimeProvider.System);
            if (caller == "hold") await repo.CreateHoldAsync(actor, new("PROJECT", project, conflict ? "changed" : "original"), key, default);
            else await repo.AdmitEvaluationAsync(actor, project, new(conflict ? [] : null), key, default);
        }
    }
    private static Task<ExportAdmissionResult> ExportAsync(RoadGuardDbContext db, Guid actor, Guid project, string key, bool conflict,
        Func<Guid, DateTimeOffset, CancellationToken, Task<ExportCaptureResult>> capture, CancellationToken token = default)
        => new ExportRepository(db, new(db), TimeProvider.System).AdmitAsync(actor, project, new("DOSSIER", "PDF"), key, new string(conflict ? 'b' : 'a', 64), null, capture, token);
    private static ExportCaptureResult Capture(Guid id, Guid actor, Guid project, DateTimeOffset at)
        => new(null, new(new("anh02.export.v1", id, project, "DOSSIER", "PDF", actor, at, at, [], new("DOSSIER", "PDF"), [], [], [], null, ""), null));
    private async Task<(int Receipts, int Audits, int Jobs, int Runs, int Holds, int Evaluations)> CountsAsync(Guid actor, Guid project)
    {
        await using var db = sql.CreateDbContext();
        return (await db.Set<IdempotencyRecord>().CountAsync(x => x.ActorUserId == actor), await db.AuditLogs.CountAsync(x => x.ActorUserId == actor),
            await db.Set<ExportJob>().CountAsync(x => x.ProjectId == project), await db.Set<RoadGuardSystem.BusinessObjects.Processing.AiMockRun>().CountAsync(x => x.ProjectId == project),
            await db.Set<RetentionHold>().CountAsync(x => x.CreatedBy == actor), await db.Set<RetentionEvaluation>().CountAsync(x => x.ProjectId == project));
    }
    internal sealed class ReceiptHook(Func<DbCommand, CancellationToken, Task> callback) : DbCommandInterceptor
    {
        public int Calls { get; private set; }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (Calls == 0 && command.CommandText.Contains("FROM [IdempotencyRecords]", StringComparison.Ordinal)) { Calls++; await callback(command, cancellationToken); }
            return result;
        }
    }
    private sealed class AuthorityFailure(Exception failure, CancellationToken expected) : DbCommandInterceptor
    {
        public int Calls { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("[Users] WITH", StringComparison.Ordinal))
            { Assert.NotNull(eventData.Context!.Database.CurrentTransaction); Assert.Equal(expected, cancellationToken); Calls++; throw failure; }
            return ValueTask.FromResult(result);
        }
    }
    private sealed class AfterCommit(Func<Task> action) : DbTransactionInterceptor
    {
        private bool fired;
        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        { if (!fired) { fired = true; await action(); } }
    }
}
