namespace RoadGuardSystem.Repositories.Surveys;

public interface ISurveyAssessmentRepository
{
    Task<AssessmentRecord?> ReadAsync(Guid dataset, Guid? assessment, CancellationToken token);
    Task<AssessmentOutcome> CreateAsync(Guid actor, Guid project, Guid dataset, string version, IReadOnlyList<AssessmentItem> items,
        string key, string fingerprint, Guid? correlation, CancellationToken token);
    Task<AssessmentOutcome> SelectAsync(Guid actor, Guid project, IReadOnlyList<BaselineItem> items, string reason,
        string key, string fingerprint, Guid? correlation, CancellationToken token);
    Task<BaselineRecord?> HistoryAsync(Guid project, Guid id, CancellationToken token);
    Task<IReadOnlyList<SelectedBaselineItem>> CurrentAsync(Guid project, Guid? segmentSetId, CancellationToken token);
}
