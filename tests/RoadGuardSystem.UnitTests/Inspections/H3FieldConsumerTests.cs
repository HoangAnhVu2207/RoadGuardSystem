using System.Reflection;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Defects;
using Xunit;
namespace RoadGuardSystem.UnitTests.Inspections;

public sealed class H3FieldConsumerTests
{
    [Theory]
    [InlineData(DefectVerificationAction.Confirm, DefectStatus.Verified)]
    [InlineData(DefectVerificationAction.Reject, DefectStatus.Rejected)]
    public void Genuine_report_defect_FIELD_decision_records_actual_task_and_submission(DefectVerificationAction action, DefectStatus terminal)
    {
        var source = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, Guid.NewGuid(), "source"), Guid.NewGuid(), "geometry");
        var defect = Defect.CreateFromReport(Guid.NewGuid(), source, CandidateClassification.Create(Guid.NewGuid(), "CRACK", null, DefectSeverity.Low, null), null, DateTimeOffset.UtcNow);
        var method = typeof(Defect).GetMethod("DecideFromField"); Assert.NotNull(method);
        var task = Guid.NewGuid(); var submission = Guid.NewGuid(); var evidence = Guid.NewGuid();
        var args = new object[] { action, task, submission, new string('a', 64), new Guid[] { evidence }, new Guid[] { evidence }, Guid.NewGuid(), "Actual FIELD review" };
        var log = Assert.IsType<DefectVerificationLog>(method.Invoke(defect, args));
        Assert.Equal(terminal, defect.Status); Assert.Equal(task, log.FieldInspectionTaskId); Assert.Null(defect.SourceAIDetectionId);
        using var after = JsonDocument.Parse(log.AfterSnapshot!); Assert.Equal("FIELD", after.RootElement.GetProperty("method").GetString());
        Assert.Equal(submission, after.RootElement.GetProperty("submissionId").GetGuid());
        var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(defect, args)); Assert.IsType<InvalidOperationException>(error.InnerException);
    }
}
