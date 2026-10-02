using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Reports;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reports;

[Trait("Package", "HUY-01")]
public sealed class Huy01ReporterCaseDomainTests
{
    [Fact]
    public void Supplement_AppendsImmutableEvidenceWithoutChangingReportOrigin()
    {
        var reporterId = Guid.NewGuid();
        var report = Report.Create(
            Guid.NewGuid(),
            reporterId,
            "Initial road damage report",
            new DateTimeOffset(2026, 10, 2, 1, 0, 0, TimeSpan.Zero),
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), Guid.NewGuid(), "file-v1", reporterId)]);

        report.AddSupplement(
            Guid.NewGuid(),
            "Additional close-up image",
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), Guid.NewGuid(), "file-v2", reporterId)],
            new DateTimeOffset(2026, 10, 2, 2, 0, 0, TimeSpan.Zero));

        report.ReporterUserId.Should().Be(reporterId);
        report.Description.Should().Be("Initial road damage report");
        report.OriginalEvidence.Should().ContainSingle();
        report.Supplements.Should().ContainSingle();
        report.Supplements.Single().Evidence.Should().ContainSingle();
        report.Supplements.Single().Description.Should().Be("Additional close-up image");
        var mutateOriginalEvidence = () => ((IList<ReportEvidence>)report.OriginalEvidence).Clear();
        mutateOriginalEvidence.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Supplement_InvalidEvidenceOwner_DoesNotAppendPartialSupplement()
    {
        var reporterId = Guid.NewGuid();
        var report = Report.Create(
            Guid.NewGuid(), reporterId, "Initial report", DateTimeOffset.UtcNow,
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), Guid.NewGuid(), "file-v1", reporterId)]);

        var append = () => report.AddSupplement(
            Guid.NewGuid(),
            "Should not be appended",
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), Guid.NewGuid(), "file-v2", Guid.NewGuid())],
            DateTimeOffset.UtcNow);

        append.Should().Throw<ArgumentException>();
        report.Supplements.Should().BeEmpty();
        report.OriginalEvidence.Should().ContainSingle();
    }

    [Fact]
    public void Supplement_ReusedEvidence_DoesNotDuplicateReportEvidenceHistory()
    {
        var reporterId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var report = Report.Create(
            Guid.NewGuid(), reporterId, "Initial report", DateTimeOffset.UtcNow,
            [VerifiedEvidenceReference.Create(evidenceId, Guid.NewGuid(), "file-v1", reporterId)]);

        var append = () => report.AddSupplement(
            Guid.NewGuid(), "Duplicate evidence", [VerifiedEvidenceReference.Create(evidenceId, Guid.NewGuid(), "file-v2", reporterId)], DateTimeOffset.UtcNow);

        append.Should().Throw<InvalidOperationException>();
        report.Supplements.Should().BeEmpty();
        report.OriginalEvidence.Should().ContainSingle();
    }

    [Fact]
    public void Case_SupplementAfterConclusion_ReopensWithoutChangingPublishedSnapshot()
    {
        var reportId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var defectId = Guid.NewGuid();
        var incidentCase = CreateOpenCase(reportId, out _);
        incidentCase.Conclude(
            CaseConclusion.Create(Guid.NewGuid(), CaseConclusionOutcome.Confirmed, [defectId], [evidenceId], "Verified defect", DateTimeOffset.UtcNow),
            CaseConclusionPrerequisites.Create([defectId], [evidenceId]));
        var publication = incidentCase.Publish(
            Guid.NewGuid(), [reportId], [defectId], [evidenceId], "Verified defect has been published.", DateTimeOffset.UtcNow,
            CasePublicationPrerequisites.Create([
                CasePublicationRecipientFacts.Create(reportId, [defectId], [evidenceId])
            ]));

        incidentCase.RegisterSupplement(reportId, DateTimeOffset.UtcNow);

        incidentCase.Status.Should().Be(IncidentCaseStatus.Open);
        incidentCase.Conclusions.Should().ContainSingle();
        incidentCase.Publications.Should().ContainSingle().Which.Id.Should().Be(publication.Id);
        publication.Summary.Should().Be("Verified defect has been published.");
    }

    [Fact]
    public void Case_ConclusionMissingVerifiedEvidence_DoesNotPartiallyMutate()
    {
        var incidentCase = CreateOpenCase(Guid.NewGuid(), out _);
        var conclusion = CaseConclusion.Create(
            Guid.NewGuid(), CaseConclusionOutcome.Confirmed, [Guid.NewGuid()], [Guid.NewGuid()], "Verified defect", DateTimeOffset.UtcNow);

        var conclude = () => incidentCase.Conclude(conclusion, CaseConclusionPrerequisites.Create([], []));

        conclude.Should().Throw<InvalidOperationException>();
        incidentCase.Status.Should().Be(IncidentCaseStatus.Open);
        incidentCase.Conclusions.Should().BeEmpty();
    }

    [Fact]
    public void Case_NoDefectConclusionWithoutVerifiedEvidence_DoesNotPartiallyMutate()
    {
        var incidentCase = CreateOpenCase(Guid.NewGuid(), out _);
        var conclusion = CaseConclusion.Create(
            Guid.NewGuid(), CaseConclusionOutcome.NoDefect, [], [], "No defect found", DateTimeOffset.UtcNow);

        var conclude = () => incidentCase.Conclude(conclusion, CaseConclusionPrerequisites.Create([], []));

        conclude.Should().Throw<InvalidOperationException>();
        incidentCase.Status.Should().Be(IncidentCaseStatus.Open);
        incidentCase.Conclusions.Should().BeEmpty();
    }

    [Fact]
    public void Case_LinkAcrossProjects_IsRejectedWithoutMovingTheSourceReport()
    {
        var sourceReportId = Guid.NewGuid();
        var source = CreateOpenCase(sourceReportId, out _);
        var target = CreateOpenCase(Guid.NewGuid(), out _);

        var move = () => target.LinkReportsFrom(source, [sourceReportId], Guid.NewGuid(), "same issue", DateTimeOffset.UtcNow);

        move.Should().Throw<InvalidOperationException>();
        source.ActiveReportIds.Should().ContainSingle().Which.Should().Be(sourceReportId);
        target.ActiveReportIds.Should().ContainSingle();
        source.Status.Should().Be(IncidentCaseStatus.Open);
    }

    [Fact]
    public void Case_SplitRequiresProperSubsetAndCreatesOpenSameProjectCase()
    {
        var firstReportId = Guid.NewGuid();
        var secondReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var source = CreateOpenCase(firstReportId, projectId);
        var donor = CreateOpenCase(secondReportId, projectId);
        source.LinkReportsFrom(donor, [secondReportId], Guid.NewGuid(), "same project source consolidation", DateTimeOffset.UtcNow);

        var split = source.SplitReports(
            Guid.NewGuid(), [secondReportId], Guid.NewGuid(), "separate defect", DateTimeOffset.UtcNow);

        split.ProjectId.Should().Be(projectId);
        split.Status.Should().Be(IncidentCaseStatus.Open);
        split.ActiveReportIds.Should().ContainSingle().Which.Should().Be(secondReportId);
        source.ActiveReportIds.Should().ContainSingle().Which.Should().Be(firstReportId);
        var splitAll = () => source.SplitReports(Guid.NewGuid(), [firstReportId], Guid.NewGuid(), "invalid all", DateTimeOffset.UtcNow);
        splitAll.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Case_SplitUsingSourceIdentity_IsRejectedWithoutMutatingSourceOrHistory()
    {
        var firstReportId = Guid.NewGuid();
        var secondReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var source = CreateOpenCase(firstReportId, projectId);
        var donor = CreateOpenCase(secondReportId, projectId);
        source.LinkReportsFrom(donor, [secondReportId], Guid.NewGuid(), "same project source consolidation", DateTimeOffset.UtcNow);
        var reportsBefore = source.ActiveReportIds.ToArray();
        var historyBefore = source.LinkHistory.ToArray();

        var split = () => source.SplitReports(source.Id, [secondReportId], Guid.NewGuid(), "invalid self identity", DateTimeOffset.UtcNow);

        split.Should().Throw<InvalidOperationException>();
        source.ActiveReportIds.Should().Equal(reportsBefore);
        source.Status.Should().Be(IncidentCaseStatus.Open);
        source.LinkHistory.Should().Equal(historyBefore);
    }

    [Fact]
    public void Case_PublicationRequiresVerifiedDefectWhileCaseMayRemainOpen()
    {
        var reportId = Guid.NewGuid();
        var incidentCase = CreateOpenCase(reportId, out _);
        var defectId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        var publish = () => incidentCase.Publish(
            Guid.NewGuid(), [reportId], [defectId], [evidenceId], "A partial update", DateTimeOffset.UtcNow,
            CasePublicationPrerequisites.Create([
                CasePublicationRecipientFacts.Create(reportId, [], [evidenceId])
            ]));

        publish.Should().Throw<InvalidOperationException>();
        incidentCase.Publications.Should().BeEmpty();
        incidentCase.Status.Should().Be(IncidentCaseStatus.Open);
    }

    [Fact]
    public void Case_PublicationRejectsEvidenceNotPermittedForEverySelectedRecipient()
    {
        var firstReportId = Guid.NewGuid();
        var secondReportId = Guid.NewGuid();
        var incidentCase = CreateOpenCaseWithTwoReports(firstReportId, secondReportId);
        var defectId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var prerequisites = CasePublicationPrerequisites.Create([
            CasePublicationRecipientFacts.Create(firstReportId, [defectId], [evidenceId]),
            CasePublicationRecipientFacts.Create(secondReportId, [defectId], [])
        ]);

        var publishForBoth = () => incidentCase.Publish(
            Guid.NewGuid(), [firstReportId, secondReportId], [defectId], [evidenceId], "A partial update", DateTimeOffset.UtcNow, prerequisites);

        publishForBoth.Should().Throw<InvalidOperationException>();
        incidentCase.Publications.Should().BeEmpty();

        var publicationForFirstOnly = incidentCase.Publish(
            Guid.NewGuid(), [firstReportId], [defectId], [evidenceId], "A partial update", DateTimeOffset.UtcNow, prerequisites);

        publicationForFirstOnly.RecipientReportIds.Should().ContainSingle().Which.Should().Be(firstReportId);
        incidentCase.Publications.Should().ContainSingle();
    }

    [Fact]
    public void Case_PublicationRejectsVerifiedDefectNotRelatedToRecipientReport()
    {
        var firstReportId = Guid.NewGuid();
        var secondReportId = Guid.NewGuid();
        var incidentCase = CreateOpenCaseWithTwoReports(firstReportId, secondReportId);
        var defectId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var prerequisites = CasePublicationPrerequisites.Create([
            CasePublicationRecipientFacts.Create(firstReportId, [defectId], [evidenceId]),
            CasePublicationRecipientFacts.Create(secondReportId, [], [evidenceId])
        ]);

        var publish = () => incidentCase.Publish(
            Guid.NewGuid(), [secondReportId], [defectId], [evidenceId], "A partial update", DateTimeOffset.UtcNow, prerequisites);

        publish.Should().Throw<InvalidOperationException>();
        incidentCase.Publications.Should().BeEmpty();
    }

    private static IncidentCase CreateOpenCase(Guid reportId, out Guid projectId)
    {
        projectId = Guid.NewGuid();
        return CreateOpenCase(reportId, projectId);
    }

    private static IncidentCase CreateOpenCase(Guid reportId, Guid projectId)
    {
        var incidentCase = IncidentCase.CreateUnassigned(Guid.NewGuid(), reportId, DateTimeOffset.UtcNow);
        var triagedAt = new DateTimeOffset(2026, 10, 2, 3, 0, 0, TimeSpan.Zero);
        incidentCase.Triage(projectId, CaseVerificationMethod.ExistingEvidence, "Initial triage", triagedAt);
        incidentCase.TriageReason.Should().Be("Initial triage");
        incidentCase.TriagedAt.Should().Be(triagedAt);
        return incidentCase;
    }

    private static IncidentCase CreateOpenCaseWithTwoReports(Guid firstReportId, Guid secondReportId)
    {
        var projectId = Guid.NewGuid();
        var incidentCase = CreateOpenCase(firstReportId, projectId);
        var donor = CreateOpenCase(secondReportId, projectId);
        incidentCase.LinkReportsFrom(donor, [secondReportId], Guid.NewGuid(), "same project source consolidation", DateTimeOffset.UtcNow);
        return incidentCase;
    }
}
