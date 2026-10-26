using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Implementations.Cases;
using RoadGuardSystem.Services.Integration;
using Xunit;
namespace RoadGuardSystem.ApiTests.Cases;


public sealed class PublicPublicationAtomicityAcceptanceTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    private const string Password = "Current1!";
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PostSaveFailureRollsBackCreateAndWithdrawWithExactAudit(bool withdraw)
    {
        var s = await Seed("CRACK");
        var target = s.Source.Defect;
        if (withdraw)
        {
            await using var db = sql.CreateDbContext();
            var created = await Service(db).CreateAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, target, Guid.NewGuid().ToString(), default);
            Assert.Equal(201, created.Status); target = created.Publication!.PublicId;
        }
        var key = Guid.NewGuid().ToString();
        int receiptsBefore, auditsBefore, publicationsBefore;
        await using (var db = sql.CreateDbContext())
        {
            receiptsBefore = await db.IdempotencyRecords.CountAsync(x => x.ProjectId == s.Source.Project);
            auditsBefore = await db.AuditLogs.CountAsync(x => x.ActorUserId == s.Source.Pm);
            publicationsBefore = await db.Set<PublicPublication>().CountAsync(x => x.ProjectId == s.Source.Project);
        }
        await using (var db = sql.CreateDbContext())
        {
            var service = new PublicPublicationService(new FailAfterSave(new PublicPublicationRepository(db, TimeProvider.System)),
                new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System), new IdempotencyOperationService(db));
            await Assert.ThrowsAsync<InvalidOperationException>(() => withdraw
                ? service.WithdrawAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, target, key, default)
                : service.CreateAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, target, key, default));
        }
        await using (var db = sql.CreateDbContext())
        {
            Assert.Equal(receiptsBefore, await db.IdempotencyRecords.CountAsync(x => x.ProjectId == s.Source.Project));
            Assert.Equal(auditsBefore, await db.AuditLogs.CountAsync(x => x.ActorUserId == s.Source.Pm));
            Assert.Equal(publicationsBefore, await db.Set<PublicPublication>().CountAsync(x => x.ProjectId == s.Source.Project));
            if (withdraw)
            {
                var row = await db.Set<PublicPublication>().SingleAsync(x => x.Id == target);
                Assert.Null(row.WithdrawnAt); Assert.Null(row.WithdrawnBy);
                Assert.NotNull(await Service(db).ReadAsync(target, default));
            }
        }
        Guid publicId;
        var before = DateTimeOffset.UtcNow;
        await using (var db = sql.CreateDbContext())
        {
            var service = Service(db);
            var result = withdraw
                ? await service.WithdrawAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, target, key, default)
                : await service.CreateAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, target, key, default);
            Assert.Equal(withdraw ? 200 : 201, result.Status); Assert.Null(result.Code);
            publicId = result.Publication!.PublicId;
            var replay = withdraw
                ? await service.WithdrawAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, target, key, default)
                : await service.CreateAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, target, key, default);
            Assert.Equal(result, replay);
        }
        var after = DateTimeOffset.UtcNow;
        await using (var db = sql.CreateDbContext())
        {
            Assert.Equal(receiptsBefore + 1, await db.IdempotencyRecords.CountAsync(x => x.ProjectId == s.Source.Project));
            Assert.Equal(auditsBefore + 1, await db.AuditLogs.CountAsync(x => x.ActorUserId == s.Source.Pm));
            Assert.Equal(publicationsBefore + (withdraw ? 0 : 1), await db.Set<PublicPublication>().CountAsync(x => x.ProjectId == s.Source.Project));
            var kind = withdraw ? "public_publication_withdrawn" : "public_publication_created";
            var audit = await db.AuditLogs.SingleAsync(x => x.EntityId == publicId && x.EventType == kind);
            Assert.Equal(s.Source.Pm, audit.ActorUserId); Assert.Equal("PublicPublication", audit.EntityType);
            Assert.Equal("c1.publication", audit.Source); Assert.InRange(audit.OccurredAtUtc, before, after);
            Assert.Null(audit.BeforeSnapshot); Assert.Null(audit.Reason); Assert.Null(audit.CorrelationId);
            var json = JsonDocument.Parse(audit.AfterSnapshot!).RootElement;
            Assert.Equal(new[] { "publicId" }, json.EnumerateObject().Select(x => x.Name).ToArray());
            Assert.Equal(publicId, json.GetProperty("publicId").GetGuid());
            var row = await db.Set<PublicPublication>().SingleAsync(x => x.Id == publicId);
            Assert.Equal(s.Source.Pm, row.ApprovedBy); Assert.Equal(s.Source.Defect, row.DefectId);
            if (withdraw)
            { Assert.Equal(s.Source.Pm, row.WithdrawnBy); Assert.NotNull(row.WithdrawnAt); Assert.Null(await Service(db).ReadAsync(publicId, default)); }
            else
            { Assert.Null(row.WithdrawnAt); Assert.InRange(row.PublishedAt, before, after); }
            Assert.True(await db.Files.AnyAsync(x => x.Id == s.File));
        }
    }
    private sealed class FailAfterSave(IPublicPublicationRepository inner) : IPublicPublicationRepository
    {
        public Task GuardAsync(Guid a, UserRoleCode r, Guid p, Guid t, bool w, Func<Guid, CancellationToken, Task<bool>> access, CancellationToken ct)
            => inner.GuardAsync(a, r, p, t, w, access, ct);
        public async Task<PublicPublication> CreateAsync(Guid a, Guid p, Guid d, CancellationToken ct)
        { await inner.CreateAsync(a, p, d, ct); throw new InvalidOperationException("Owned create post-save failure"); }
        public async Task<PublicPublication> WithdrawAsync(Guid a, Guid p, Guid id, CancellationToken ct)
        { await inner.WithdrawAsync(a, p, id, ct); throw new InvalidOperationException("Owned withdraw post-save failure"); }
        public Task<PublicPublication?> ReadAsync(Guid id, CancellationToken ct) => inner.ReadAsync(id, ct);
    }
    private async Task<Setup> Seed(string code, string? mutation = null)
    {
        await using var db = sql.CreateDbContext(); var source = await GenuineProtectedSource.Seed(db, sql);
        if (!await db.DefectTypes.AnyAsync(x => x.Code == code)) { db.DefectTypes.Add(DefectType.Create(code, "Owner allowlisted test catalog")); await db.SaveChangesAsync(); }
        await db.DefectTypes.Where(x => x.Code == code).ExecuteUpdateAsync(x => x.SetProperty(v => v.IsActive, true));
        var incident = await db.IncidentCases.SingleAsync(x => x.ProjectId == source.Project);
        var report = await db.Reports.Include(x => x.OriginalEvidence).SingleAsync(x => x.Id == incident.ActiveReportIds.Single());
        var evidence = report.OriginalEvidence.Single(); var defect = await db.Defects.SingleAsync(x => x.Id == source.Defect);
        db.DefectVerificationLogs.Add(defect.Assess(code, null, DefectSeverity.Low, source.Pm, "Allowlisted assessment", [evidence.Id]));
        if (mutation != "no-provenance") db.DefectVerificationLogs.Add(defect.DecideFromExistingEvidence(DefectVerificationAction.Confirm, [evidence.Id], [evidence.Id], source.Pm, "Confirmed retained source"));
        await db.SaveChangesAsync();
        if (mutation == "no-provenance") await db.Defects.Where(x => x.Id == defect.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, DefectStatus.Verified));
        var caseService = new CaseWorkflowService(new CaseWorkflowRepository(db), new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System),
            new AnhHuyProducerService(new AnhHuyFactsRepository(db), null!, null!), new IdempotencyOperationService(db));
        var read = await caseService.ReadAsync(source.Pm, UserRoleCode.ProjectManager, incident.Id, default);
        var concluded = await caseService.CommandAsync(source.Pm, UserRoleCode.ProjectManager, new(incident.Id, read.Case!.Version, "conclude", "Confirmed publication source",
            Outcome: mutation == "unconfirmed-case" ? CaseConclusionOutcome.NoDefect : CaseConclusionOutcome.Confirmed, DefectIds: mutation == "unconfirmed-case" ? [] : [defect.Id], EvidenceIds: [evidence.Id]), Guid.NewGuid().ToString(), null, default);
        Assert.Equal(200, concluded.Status); return new(source, incident.Id, evidence.FileId);
    }
    private static PublicPublicationService Service(RoadGuardDbContext db) => new(new PublicPublicationRepository(db, TimeProvider.System),
        new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System), new IdempotencyOperationService(db));
    private async Task<HttpClient> Client(AuthenticationWebApplicationFactory factory, Guid actor, bool web = false)
    {
        await using var db = sql.CreateDbContext(); var email = await db.Users.Where(x => x.Id == actor).Select(x => x.Email).SingleAsync();
        if (web) return await WebCookieTestClient.Login(factory, email!);
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password }); login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString()); return client;
    }
    private static string CreateRoute(Setup s) => $"/api/v1/projects/{s.Source.Project}/defects/{s.Source.Defect}/publications";
    private static Task<HttpResponseMessage> Post(HttpClient client, string route, string key)
    { var request = new HttpRequestMessage(HttpMethod.Post, route); request.Headers.Add("Idempotency-Key", key); return client.SendAsync(request); }
    private sealed record Setup(GenuineProtectedSource.Source Source, Guid Case, Guid File);
}
