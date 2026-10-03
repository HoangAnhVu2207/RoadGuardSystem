using RoadGuardSystem.Services.Candidates;
using Xunit;

namespace RoadGuardSystem.UnitTests.Candidates;

[Trait("Package", "HUY-01")]
public sealed class Huy01CandidateMatcherTests
{
    [Fact]
    public void Matching_NoGps_KeepsAssignedAndNeighborScopeAndNeverExpandsAcrossProjects()
    {
        var project = Guid.NewGuid(); var assigned = Guid.NewGuid(); var neighbor = Guid.NewGuid(); var distant = Guid.NewGuid();
        var first = new CandidateMatchFact(Guid.NewGuid(), project, "v1", assigned, null, false);
        var second = new CandidateMatchFact(Guid.NewGuid(), project, "v1", neighbor, null, true);
        var third = new CandidateMatchFact(Guid.NewGuid(), project, "v1", distant, null, false);
        var wrong = new CandidateMatchFact(Guid.NewGuid(), Guid.NewGuid(), "v1", assigned, 0, true);
        var result = CandidateMatcher.Match(project, [assigned], [neighbor], [third, wrong, second, first], false, false);
        Assert.Equal(new[] { first.DefectId, second.DefectId }, result.Select(r => r.DefectId));
        Assert.All(result, r => { Assert.Null(r.DistanceMeters); Assert.Contains("GPS_MISSING", r.ReasonCodes); });
        var expanded = CandidateMatcher.Match(project, [assigned], [neighbor], [third, wrong, second, first], true, false);
        Assert.Equal(new[] { first.DefectId, second.DefectId, third.DefectId }, expanded.Select(r => r.DefectId));
    }

    [Fact]
    public void Matching_MetricDistanceAndTieBreak_AreDeterministicWithoutInventedScores()
    {
        var p = Guid.NewGuid(); var s = Guid.NewGuid(); var lowId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var highId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var facts = new[] { new CandidateMatchFact(highId, p, "v", s, 4, false), new CandidateMatchFact(lowId, p, "v", s, 4, false) };
        Assert.Equal(new[] { lowId, highId }, CandidateMatcher.Match(p, [s], [], facts, false, true).Select(x => x.DefectId));
        Assert.All(CandidateMatcher.Match(p, [s], [], facts, false, false), x => Assert.Null(x.DistanceMeters));
    }
}
