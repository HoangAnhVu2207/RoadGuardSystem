using RoadGuardSystem.Repositories.Files;

namespace RoadGuardSystem.Services.Files;

public sealed class UploadVerificationRound
{
    private IReadOnlyList<Guid>? _candidates;
    private int _next;

    public async Task<UploadPersistenceStatus> ProcessOneAsync(IUploadRepository repository, CancellationToken cancellationToken = default)
    {
        if (_candidates is null)
        {
            _candidates = await repository.GetVerifyingIdsAsync(cancellationToken);
            _next = 0;
        }

        if (_next == _candidates.Count)
        {
            _candidates = null;
            return UploadPersistenceStatus.NotFound;
        }

        var result = await repository.VerifyAsync(_candidates[_next], cancellationToken);
        _next++;
        if (_next == _candidates.Count) _candidates = null;
        return result;
    }
}
