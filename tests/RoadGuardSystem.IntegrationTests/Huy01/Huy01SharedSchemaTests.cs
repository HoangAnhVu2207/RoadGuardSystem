using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Models.Huy01;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Huy01;

[Trait("Package", "HUY-01")]
public sealed class Huy01SharedSchemaTests
{
    [Fact]
    public void Domain_report_is_mapped_without_competing_entity()
    {
        using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer("Server=localhost;Database=model-only;Integrated Security=true", o => o.UseNetTopologySuite()).Options);
        db.Model.FindEntityType(typeof(Report)).Should().NotBeNull();
        db.Model.FindEntityType(typeof(Report))!.GetTableName().Should().Be("Reports");
    }

    [Fact]
    public async Task Empty_integration_down_and_up_restores_additive_schema_without_touching_baseline_history()
    {
        var fixture = new SqlServerTestFixture(createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        try
        {
            await using var db = Context(fixture.ConnectionString);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261002120000_AnhHuySharedIntegration");
            await migrator.MigrateAsync("20261002100000_Anh01RequestScopeRootCorrection");
            (await db.Database.GetAppliedMigrationsAsync()).Should().NotContain("20261002120000_AnhHuySharedIntegration");
            (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002100000_Anh01RequestScopeRootCorrection");
            await migrator.MigrateAsync("20261002120000_AnhHuySharedIntegration");
            (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002120000_AnhHuySharedIntegration");
            (await db.Set<Report>().CountAsync()).Should().Be(0);
            (await db.Set<IncidentCase>().CountAsync()).Should().Be(0);
            (await db.Set<HuyCandidateSourceHead>().CountAsync()).Should().Be(0);
            await db.Database.MigrateAsync();
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Fresh_or_baseline_upgrade_roundtrips_domain_and_enforces_relational_heads(bool baselineUpgrade)
    {
        var fixture = new SqlServerTestFixture(createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        try
        {
            await using var db = Context(fixture.ConnectionString);
            if (baselineUpgrade)
                await db.GetService<IMigrator>().MigrateAsync("20261002100000_Anh01RequestScopeRootCorrection");
            await db.Database.MigrateAsync();
            var actor = Guid.NewGuid(); var project = Guid.NewGuid();
            await SeedActorProject(db, actor, project);
            var file = StoredFile.Create(Guid.NewGuid(), "s3://fixture/evidence", "photo.jpg", "image/jpeg", 123, new string('a', 64), actor, DateTimeOffset.UtcNow, null);
            var secondFile = StoredFile.Create(Guid.NewGuid(), "s3://fixture/supplement", "supplement.jpg", "image/jpeg", 456, new string('b', 64), actor, DateTimeOffset.UtcNow, null);
            db.Files.AddRange(file, secondFile); await db.SaveChangesAsync();
            var originalId = Guid.NewGuid(); var supplementEvidenceId = Guid.NewGuid();
            var report = Report.Create(Guid.NewGuid(), actor, "Original immutable report", DateTimeOffset.UtcNow,
                [VerifiedEvidenceReference.Create(originalId, file.Id, "file-v1", actor, EvidenceCaptureMetadata.Create(DateTimeOffset.UtcNow, EvidenceLocationSource.Unknown))]);
            report.AddSupplement(Guid.NewGuid(), "Separate immutable supplement", [VerifiedEvidenceReference.Create(supplementEvidenceId, secondFile.Id, "file-v2", actor)], DateTimeOffset.UtcNow);
            db.Set<Report>().Add(report); await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var loaded = await db.Set<Report>().Include(x => x.OriginalEvidence).Include(x => x.Supplements).ThenInclude(x => x.Evidence).SingleAsync(x => x.Id == report.Id);
            loaded.OriginalEvidence.Should().ContainSingle(x => x.Id == originalId);
            loaded.Supplements.Should().ContainSingle().Which.Evidence.Should().ContainSingle(x => x.Id == supplementEvidenceId);
            var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, DateTimeOffset.UtcNow);
            incident.Triage(project, CaseVerificationMethod.ExistingEvidence, "Fixture triage", DateTimeOffset.UtcNow);
            db.Set<IncidentCase>().Add(incident);
            db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = incident.Id, ReportId = report.Id, StartedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            var initialVersion = db.Entry(incident).Property<byte[]>("RowVersion").CurrentValue!.ToArray();
            db.Entry(incident).Property<long>("Revision").CurrentValue++;
            await db.SaveChangesAsync();
            db.Entry(incident).Property<byte[]>("RowVersion").CurrentValue.Should().NotEqual(initialVersion);
            var conclusion = CaseConclusion.Create(Guid.NewGuid(), CaseConclusionOutcome.NoDefect, [], [originalId], "Evidence found no defect", DateTimeOffset.UtcNow);
            incident.Conclude(conclusion, CaseConclusionPrerequisites.Create([], [originalId]));
            var publication = incident.Publish(Guid.NewGuid(), [report.Id], [], [originalId], "Private recipient publication", DateTimeOffset.UtcNow,
                CasePublicationPrerequisites.Create([CasePublicationRecipientFacts.Create(report.Id, [], [originalId])]));
            db.Entry(incident).Property<long>("Revision").CurrentValue++;
            db.Set<HuyConclusionEvidence>().Add(new() { ConclusionId = conclusion.Id, EvidenceId = originalId, SourceReportId = report.Id, OriginalEvidenceId = originalId });
            db.Set<HuyPublicationRecipient>().Add(new() { PublicationId = publication.Id, ReportId = report.Id });
            db.Set<HuyPublicationEvidence>().Add(new() { PublicationId = publication.Id, RecipientReportId = report.Id, EvidenceId = originalId, SourceReportId = report.Id, OriginalEvidenceId = originalId });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var hydrated = await db.Set<IncidentCase>().Include(x => x.Conclusions).Include(x => x.Publications).SingleAsync(x => x.Id == incident.Id);
            hydrated.ActiveReportIds.Should().Equal(report.Id);
            hydrated.Conclusions.Should().ContainSingle().Which.EvidenceIds.Should().Equal(originalId);
            hydrated.Publications.Should().ContainSingle().Which.RecipientReportIds.Should().Equal(report.Id);

            // Schema-only negatives: no business orchestration/authorization claim.
            await RejectSql(db, $"INSERT INTO CaseReportLinks(Id,CaseId,ReportId,StartedAt) VALUES ('{Guid.NewGuid()}','{incident.Id}','{report.Id}',SYSDATETIMEOFFSET())");
            await RejectSql(db, $"UPDATE Reports SET Description=N'changed' WHERE Id='{report.Id}'");
            await RejectSql(db, $"UPDATE Reports SET Description=N'original immutable report' WHERE Id='{report.Id}'");
            await RejectSql(db, $"UPDATE Reports SET Description=N'Original immutable report ' WHERE Id='{report.Id}'");
            await RejectSql(db, $"UPDATE ReportOriginalEvidence SET FileVersion=N'changed' WHERE Id='{originalId}'");
            await RejectSql(db, $"INSERT INTO CasePublicationEvidence(PublicationId,RecipientReportId,EvidenceId,SourceReportId,OriginalEvidenceId) VALUES ('{publication.Id}','{report.Id}','{supplementEvidenceId}','{report.Id}','{supplementEvidenceId}')");
            await RejectSql(db, $"INSERT INTO CasePublicationRecipients(PublicationId,ReportId) VALUES ('{publication.Id}','{Guid.NewGuid()}')");
            await RejectSql(db, $"DELETE FROM CasePublications WHERE Id='{publication.Id}'");

            var decision = CandidateDecision.Create(Guid.NewGuid(), CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, report.Id, "report-v1"), project, "geometry-v1"), CandidateDecisionKind.Reject, null, Guid.Empty, null, null, actor, "Fixture reject", DateTimeOffset.UtcNow);
            db.Set<CandidateDecision>().Add(decision);
            db.Entry(decision).Property<CandidateSourceKind>("SourceKind").CurrentValue = CandidateSourceKind.Report;
            db.Entry(decision).Property<Guid>("SourceId").CurrentValue = report.Id;
            db.Entry(decision).Property<Guid?>("ReportSourceId").CurrentValue = report.Id;
            var head = new HuyCandidateSourceHead { SourceKind = CandidateSourceKind.Report, SourceId = report.Id, ProjectId = project, DecisionId = decision.Id };
            db.Set<HuyCandidateSourceHead>().Add(head); await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var decisionRead = await db.Set<CandidateDecision>().SingleAsync(x => x.Id == decision.Id);
            decisionRead.Source.Id.Should().Be(report.Id); decisionRead.Source.SourceVersion.Should().Be("report-v1");
            await RejectSql(db, $"INSERT INTO CandidateSourceHeads(SourceKind,SourceId,ProjectId,DecisionId) VALUES (1,'{report.Id}','{project}','{decision.Id}')");
            await RejectSql(db, $"UPDATE CandidateSourceHeads SET SourceId='{Guid.NewGuid()}' WHERE SourceId='{report.Id}'");
            await RejectSql(db, $"UPDATE SourceDecisions SET Reason=N'changed' WHERE Id='{decision.Id}'");
            var wrongProject = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Projects (Id,ProjectCode,Name,Status,CreatedAt) VALUES ({wrongProject},{wrongProject.ToString()},{"Wrong-project fixture"},1,{DateTimeOffset.UtcNow})");
            await RejectSql(db, $"UPDATE CandidateSourceHeads SET ProjectId='{wrongProject}' WHERE SourceId='{report.Id}'");
            foreach (var unsupportedKind in new[] { 0, 3 })
                await RejectSql(db, $"INSERT INTO SourceDecisions(Id,ProjectId,GeometryVersion,Decision,DecidedByUserId,Reason,DecidedAt,SourceKind,SourceId,ReportSourceId,Source_Kind,Source_Id,Source_Version) SELECT '{Guid.NewGuid()}',ProjectId,GeometryVersion,Decision,DecidedByUserId,Reason,DecidedAt,{unsupportedKind},SourceId,ReportSourceId,{unsupportedKind},Source_Id,Source_Version FROM SourceDecisions WHERE Id='{decision.Id}'");
            await RejectSql(db, $"INSERT INTO SourceDecisions(Id,ProjectId,GeometryVersion,Decision,DecidedByUserId,Reason,DecidedAt,SourceKind,SourceId,ReportSourceId,Source_Kind,Source_Id,Source_Version) SELECT '{Guid.NewGuid()}',ProjectId,GeometryVersion,Decision,DecidedByUserId,Reason,DecidedAt,SourceKind,'{Guid.NewGuid()}',ReportSourceId,Source_Kind,Source_Id,Source_Version FROM SourceDecisions WHERE Id='{decision.Id}'");
            var otherReport = Report.Create(Guid.NewGuid(), actor, "Other independent source", DateTimeOffset.UtcNow,
                [VerifiedEvidenceReference.Create(Guid.NewGuid(), file.Id, "file-v1", actor)]);
            db.Set<Report>().Add(otherReport); await db.SaveChangesAsync();
            await RejectSql(db, $"INSERT INTO SourceDecisions(Id,ProjectId,GeometryVersion,Decision,DecidedByUserId,Reason,DecidedAt,SourceKind,SourceId,ReportSourceId,Source_Kind,Source_Id,Source_Version,SupersedesDecisionId,ExpectedPreviousDecisionVersion) SELECT '{Guid.NewGuid()}',ProjectId,GeometryVersion,Decision,DecidedByUserId,Reason,DecidedAt,SourceKind,'{otherReport.Id}','{otherReport.Id}',Source_Kind,'{otherReport.Id}',Source_Version,Id,N'previous-version' FROM SourceDecisions WHERE Id='{decision.Id}'");
            await RejectSql(db, $"INSERT INTO SourceDecisions(Id,ProjectId,GeometryVersion,Decision,DecidedByUserId,Reason,DecidedAt,SourceKind,SourceId,ReportSourceId,Source_Kind,Source_Id,Source_Version,SupersedesDecisionId,ExpectedPreviousDecisionVersion) SELECT '{Guid.NewGuid()}','{wrongProject}',GeometryVersion,Decision,DecidedByUserId,Reason,DecidedAt,SourceKind,SourceId,ReportSourceId,Source_Kind,Source_Id,Source_Version,Id,N'previous-version' FROM SourceDecisions WHERE Id='{decision.Id}'");

            await using (var loser = Context(fixture.ConnectionString))
            {
                var staleHead = await loser.Set<HuyCandidateSourceHead>().SingleAsync(x => x.SourceKind == CandidateSourceKind.Report && x.SourceId == report.Id);
                var currentHead = await db.Set<HuyCandidateSourceHead>().SingleAsync(x => x.SourceKind == CandidateSourceKind.Report && x.SourceId == report.Id);
                db.Entry(currentHead).Property(x => x.DecisionId).IsModified = true;
                await db.SaveChangesAsync();
                loser.Entry(staleHead).Property(x => x.DecisionId).IsModified = true;
                Func<Task> staleWrite = () => loser.SaveChangesAsync();
                await staleWrite.Should().ThrowAsync<DbUpdateConcurrencyException>();
            }

            await using (var loser = Context(fixture.ConnectionString))
            {
                var staleCase = await loser.Set<IncidentCase>().SingleAsync(x => x.Id == incident.Id);
                var currentCase = await db.Set<IncidentCase>().SingleAsync(x => x.Id == incident.Id);
                db.Entry(currentCase).Property<long>("Revision").CurrentValue++;
                await db.SaveChangesAsync();
                loser.Entry(staleCase).Property<long>("Revision").CurrentValue++;
                Func<Task> staleWrite = () => loser.SaveChangesAsync();
                await staleWrite.Should().ThrowAsync<DbUpdateConcurrencyException>();
            }

            var rolledBackCase = Guid.NewGuid();
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                var created = IncidentCase.CreateUnassigned(rolledBackCase, report.Id, DateTimeOffset.UtcNow);
                db.Set<IncidentCase>().Add(created); await db.SaveChangesAsync();
                await transaction.RollbackAsync();
            }
            db.ChangeTracker.Clear();
            (await db.Set<IncidentCase>().AnyAsync(x => x.Id == rolledBackCase)).Should().BeFalse();
            // Retry in a new unit of work after rollback must remain usable.
            db.Set<IncidentCase>().Add(IncidentCase.CreateUnassigned(rolledBackCase, report.Id, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
            (await db.Set<IncidentCase>().AnyAsync(x => x.Id == rolledBackCase)).Should().BeTrue();
            var closedLink = await db.Set<HuyCaseReportLink>().SingleAsync(x => x.ReportId == report.Id && x.EndedAt == null);
            await RejectSql(db, $"DELETE FROM CaseReportLinks WHERE Id='{closedLink.Id}'");
            await RejectSql(db, $"UPDATE CaseReportLinks SET CaseId='{rolledBackCase}' WHERE Id='{closedLink.Id}'");
            await RejectSql(db, $"UPDATE CaseReportLinks SET StartedAt=DATEADD(second,1,StartedAt) WHERE Id='{closedLink.Id}'");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE CaseReportLinks SET EndedAt=DATEADD(second,100,StartedAt) WHERE Id={closedLink.Id}");
            await RejectSql(db, $"UPDATE CaseReportLinks SET EndedAt=NULL WHERE Id='{closedLink.Id}'");
            await RejectSql(db, $"UPDATE CaseReportLinks SET EndedAt=DATEADD(second,120,StartedAt) WHERE Id='{closedLink.Id}'");
            Func<Task> destructiveDown = () => db.GetService<IMigrator>().MigrateAsync("20261002100000_Anh01RequestScopeRootCorrection");
            await destructiveDown.Should().ThrowAsync<SqlException>().Where(x => x.Number == 51130);
            (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002120000_AnhHuySharedIntegration");
            (await db.Set<Report>().AnyAsync(x => x.Id == report.Id)).Should().BeTrue();
            (await db.Set<CandidateDecision>().AnyAsync(x => x.Id == decision.Id)).Should().BeTrue();
        }
        finally { await fixture.DisposeAsync(); }
    }

    private static RoadGuardDbContext Context(string connection) => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(connection, o => o.UseNetTopologySuite()).Options);

    private static async Task RejectSql(RoadGuardDbContext db, string sql)
    {
        Func<Task> act = async () => { await db.Database.ExecuteSqlRawAsync(sql); };
        await act.Should().ThrowAsync<SqlException>();
    }

    private static async Task SeedActorProject(RoadGuardDbContext db, Guid actor, Guid project)
    {
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Roles (Code,Name,NormalizedName,IsActive) VALUES ('PM','Project manager','PM',1)");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Users (Id,UserName,NormalizedUserName,DisplayName,RoleCode,Status,MustChangePassword,CreatedAt,Email,NormalizedEmail,PasswordHash,SecurityStamp,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount) VALUES ({actor},{actor.ToString()},{actor.ToString().ToUpperInvariant()},{"Isolated schema fixture"},'PM',1,0,{DateTimeOffset.UtcNow},{"schema@example.test"},{"SCHEMA@EXAMPLE.TEST"},{"test-only-hash"},{actor.ToString()},0,0,0,0,0)");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Projects (Id,ProjectCode,Name,Status,CreatedAt) VALUES ({project},{project.ToString()},{"Shared schema fixture"},1,{DateTimeOffset.UtcNow})");
    }
}
