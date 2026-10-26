using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class B2AssignmentSnapshotTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void NewProofRejectsAmbiguousPartialVersionOrRecipientSubstitution(int invalid)
    {
        var at = DateTimeOffset.UtcNow; var id = Guid.NewGuid(); var pm = Guid.NewGuid();
        var snapshot = new NotificationAssignmentSnapshot(1, at, [pm], Guid.NewGuid(), Guid.NewGuid(), "AQIDBAUGBwg=");
        var source = new H6StoredEvent(1, id, "SUBMITTED", Guid.NewGuid(), "RepairWork", Guid.NewGuid(), id, at, id, NotificationAssignmentSnapshot: snapshot);
        Assert.NotNull(H6NotificationCatalog.Parse(id, "repair.work.submitted.v1", at, JsonSerializer.Serialize(source, Json)).Source.NotificationAssignmentSnapshot);
        source = invalid switch
        {
            0 => source with { NotificationAssignmentSnapshot = snapshot with { SchemaVersion = 2 } },
            1 => source with { NotificationAssignmentSnapshot = snapshot with { AssignmentId = null } },
            2 => source with { NotificationAssignmentSnapshot = snapshot with { AssignmentVersion = "AQIDBA==" } },
            3 => source with { NotificationAssignmentSnapshot = snapshot with { ProjectManagerIds = [pm, pm] } },
            4 => source with { ResponsibleUserId = Guid.NewGuid() },
            _ => source with { FastTrackReview = new(at, [pm], null, null) }
        };
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(id, "repair.work.submitted.v1", at, JsonSerializer.Serialize(source, Json)));
    }

    [Fact]
    public void FinalRequiresIndependentProofEmptyManagerSnapshotAndUniqueNestedFields()
    {
        var at = DateTimeOffset.UtcNow; var id = Guid.NewGuid();
        var snapshot = new NotificationAssignmentSnapshot(1, at, [], null, null, null);
        var source = new H6StoredEvent(1, id, "FAST_TRACK_CONFIRMED", Guid.NewGuid(), "RepairWork", Guid.NewGuid(), id, at, id,
            NotificationAssignmentSnapshot: snapshot, FastTrackFinal: new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        string Serialize(H6StoredEvent value) => JsonSerializer.Serialize(value, Json);
        Assert.NotNull(H6NotificationCatalog.Parse(id, "repair.work.fast_track_confirmed.v1", at, Serialize(source)).Source.FastTrackFinal);
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(id, "repair.work.fast_track_confirmed.v1", at, Serialize(source with { FastTrackFinal = null })));
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(id, "repair.work.fast_track_confirmed.v1", at, Serialize(source with { NotificationAssignmentSnapshot = snapshot with { ProjectManagerIds = [Guid.NewGuid()] } })));
        var duplicate = Serialize(source).Replace("\"assignmentId\":null", "\"assignmentId\":null,\"AssignmentId\":null", StringComparison.Ordinal);
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(id, "repair.work.fast_track_confirmed.v1", at, duplicate));
    }
}
