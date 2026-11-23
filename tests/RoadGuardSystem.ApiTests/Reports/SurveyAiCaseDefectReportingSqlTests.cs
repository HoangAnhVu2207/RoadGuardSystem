using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Services.Implementations.Integration;
using RoadGuardSystem.Services.Reporting;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

[Collection(AuthenticationApiFixture.Name)]
public sealed class SurveyAiCaseDefectReportingSqlTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcceptedSurveyAiDefectContributesToReportingWithoutReporterCase(bool hasSegment)
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        var now = DateTimeOffset.UtcNow;
        var survey = Survey.Create(Guid.NewGuid(), null, source.Project, source.Route,
            SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var data = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.Draft,
            SurveyDataIntegrityStatus.Pending, null, null, "[]");
        var block = ProcessingBlock.Create(Guid.NewGuid(), data.Id, 1, "{}");
        var model = AIModelVersion.Create(Guid.NewGuid(), "reporting fixture", Guid.NewGuid().ToString("N"),
            "fixture://local", null, null, AIModelVersionStatus.Draft, null, null);
        var job = ProcessingJob.Create(Guid.NewGuid(), block.Id, model.Id, ProcessingJobStatus.Completed,
            now.AddMinutes(-1), now, null, null);
        var type = DefectType.Create("REPORT-" + Guid.NewGuid().ToString("N"), "reporting source fixture");
        db.AddRange(survey, data, block, model, job, type);
        await db.SaveChangesAsync();
        var detection = AIDetection.Create(Guid.NewGuid(), job.Id, model.Id, source.Route, null,
            type.Code, .8m, null, null, "{}");
        db.Add(detection);
        await db.SaveChangesAsync();
        Guid? segmentId = hasSegment ? await db.RoadSegments.Where(s => s.SegmentSetId == source.Set)
            .Select(s => s.Id).SingleAsync() : null;
        var accepted = await new CandidateDecisionRepository(db).SaveAcceptedAsync(source.Pm,
            CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.AiDetection,
                detection.Id, "persisted-survey-fixture"), source.Project, "accepted-geometry-pin"),
            CandidateDecisionKind.KeepNew,
            CandidateClassification.Create(source.Route, type.Code, null, DefectSeverity.Low, segmentId),
            null, null, null, "controlled PM acceptance fixture", null, default);

        var reader = new CaseDefectReadReader(new RoadGuardSystem.Repositories.Implementations.Reporting.ReportingRepository(db));
        var snapshot = await reader.CaptureAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, new());
        Assert.NotNull(snapshot);
        var fact = Assert.Single(snapshot.Defects, d => d.DefectId == accepted.DefectId);
        Assert.Equal("AI_DETECTION", fact.SourceKind);
        Assert.Equal(detection.Id, fact.SourceId);
        Assert.Equal("persisted-survey-fixture", fact.SourceVersion);
        Assert.Equal(source.Route, fact.RouteVersionId);
        Assert.Equal(hasSegment ? source.Set : (Guid?)null, fact.SegmentSetId);
        Assert.Equal(segmentId, fact.SegmentId);
        Assert.Equal("accepted-geometry-pin", fact.GeometryVersion);
        Assert.Equal("UNKNOWN", fact.PositionStatus);
        Assert.Empty(snapshot.MissingReasons);
        var capture = new ReportingCaptureDto(new("anh02.reporting.v1", source.Project, now,
            "pilot-reporting.v1", new(), [], [], "SERIALIZABLE"), [], [], [], new("PARTIAL", []));
        var consumed = CaseDefectCaptureConsumer.Apply(capture, snapshot);
        Assert.Equal(2, Assert.Single(consumed.Summary.Metrics, m => m.Code == "defectsByStatus").Value);

        var excluded = await reader.CaptureAsync(source.Pm, UserRoleCode.ProjectManager, source.Project,
            new(SegmentIds: [Guid.NewGuid()]));
        Assert.NotNull(excluded);
        Assert.Empty(excluded.Defects);
        Assert.Empty(excluded.MissingReasons);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => reader.CaptureAsync(source.Pm,
            UserRoleCode.ProjectManager, Guid.NewGuid(), new()));

        // SQL allows a link to another existing Defect; reject that inconsistent lineage.
        var reportLink = await db.Set<HuyDefectSourceLink>().SingleAsync(l => l.DefectId == source.Defect);
        reportLink.EndedAt = now;
        var aiLink = await db.Set<HuyDefectSourceLink>().SingleAsync(l => l.DefectId == accepted.DefectId);
        aiLink.DefectId = source.Defect;
        await db.SaveChangesAsync();
        var wrongDefect = await reader.CaptureAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, new());
        Assert.NotNull(wrongDefect);
        Assert.DoesNotContain(wrongDefect.Defects, d => d.DefectId == source.Defect);

        // A typed-source FK alone does not establish an accepted decision.
        var rejected = CandidateDecision.Create(Guid.NewGuid(), CandidateSourceFacts.Create(
                CandidateSourceIdentity.Create(CandidateSourceKind.AiDetection, detection.Id, "rejected-source"),
                source.Project, "rejected-geometry"), CandidateDecisionKind.Reject, null, Guid.Empty, null,
            null, source.Pm, "negative reporting fixture", now);
        db.SourceDecisions.Add(rejected);
        db.Entry(rejected).Property<CandidateSourceKind>("SourceKind").CurrentValue = CandidateSourceKind.AiDetection;
        db.Entry(rejected).Property<Guid>("SourceId").CurrentValue = detection.Id;
        db.Entry(rejected).Property<Guid?>("AIDetectionSourceId").CurrentValue = detection.Id;
        aiLink.DefectId = accepted.DefectId!.Value;
        aiLink.DecisionId = rejected.Id;
        await db.SaveChangesAsync();
        var rejectedCapture = await reader.CaptureAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, new());
        Assert.NotNull(rejectedCapture);
        Assert.DoesNotContain(rejectedCapture.Defects, d => d.DefectId == accepted.DefectId);
    }
}
