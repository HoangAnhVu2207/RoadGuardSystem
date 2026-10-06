using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6NotificationCatalogTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset At = new(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);
    [Theory]
    [InlineData("field.task.assigned.v1", "ASSIGNED", NotificationEventKind.FieldAssigned)]
    [InlineData("field.task.assigned.v1", "REASSIGNED", NotificationEventKind.FieldAssigned)]
    [InlineData("field.task.submitted.v1", "SUBMITTED", NotificationEventKind.FieldSubmitted)]
    [InlineData("field.task.supplement_requested.v1", "SUPPLEMENT", NotificationEventKind.FieldSupplementRequested)]
    public void ActualFieldProducerShapeMapsToFiniteNotificationKind(string type, string action, NotificationEventKind expected)
    {
        var value = Event(action); var result = Parse(type, value);
        Assert.Equal(expected, result.Envelope!.Kind); Assert.False(result.AuditOnly);
        Assert.Equal(value.SourceId, result.Envelope.SourceId); Assert.Equal(value.OriginEventId, result.Envelope.OriginEventId);
    }
    [Theory]
    [InlineData("ACCEPTED")]
    [InlineData("REJECTED")]
    [InlineData("STARTED")]
    [InlineData("REVIEWED")]
    [InlineData("CANCELLED")]
    [InlineData("IMPACT_CONTINUE")]
    [InlineData("IMPACT_VERIFY")]
    public void OnlyExplicitNonR28LifecycleKindsHaveAuditOnlyPlans(string action)
    {
        var result = Parse("field.task.lifecycle.v1", Event(action));
        Assert.True(result.AuditOnly); Assert.Null(result.Envelope);
    }
    [Theory]
    [InlineData("field.task.lifecycle.v1", "SUBMITTED")]
    [InlineData("field.task.lifecycle.v1", "UNREGISTERED")]
    [InlineData("field.task.submitted.v1", "ASSIGNED")]
    public void TypeActionMismatchCannotBeSilentlyConsumed(string type, string action)
        => Assert.Throws<H6NotificationProtocolException>(() => Parse(type, Event(action)));
    [Fact]
    public void UnknownAndOtherConsumerTypesAreNotRegistered()
    {
        var value = Event("ASSIGNED");
        Assert.Throws<H6NotificationProtocolException>(() => Parse("processing.unknown.v1", value));
        Assert.DoesNotContain("processing.unknown.v1", H6NotificationCatalog.MessageTypes);
        Assert.DoesNotContain("Anh02.Export.Create", H6NotificationCatalog.MessageTypes);
    }
    [Theory]
    [InlineData("id")]
    [InlineData("time")]
    [InlineData("schema")]
    [InlineData("scope")]
    [InlineData("revision")]
    public void MalformedOrUnboundEnvelopeIsRejected(string problem)
    {
        var value = Event("SUBMITTED");
        value = problem switch {
            "schema" => value with { SchemaVersion = 2 }, "scope" => value with { SourceKind = "RepairWork" },
            "revision" => value with { SourceRevisionId = null }, _ => value };
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(problem == "id" ? Guid.NewGuid() : value.EventId,
            "field.task.submitted.v1", problem == "time" ? At.AddTicks(1) : At, JsonSerializer.Serialize(value, Json)));
    }
    [Fact]
    public void UnknownFieldsAndOversizedPayloadAreRejectedWithoutEchoingPayload()
    {
        var value = Event("ASSIGNED"); var json = JsonSerializer.Serialize(value, Json);
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(value.EventId,"field.task.assigned.v1",At,json[..^1]+",\"secret\":\"private\"}"));
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(value.EventId,"field.task.assigned.v1",At,new string('a',65537)));
    }
    private static H6NotificationEventDto Event(string action) => new(1,Guid.NewGuid(),action,Guid.NewGuid(),"FieldTask",
        Guid.NewGuid(),Guid.NewGuid(),At, action is "SUBMITTED" or "SUPPLEMENT" ? Guid.NewGuid() : null,Guid.NewGuid());
    private static H6DispatchPlan Parse(string type,H6NotificationEventDto value)
        => H6NotificationCatalog.Parse(value.EventId,type,At,JsonSerializer.Serialize(value,Json));

    [Fact]
    public void NumericSourceKindIsNotAnAcceptedWireSourceName()
        => Assert.Throws<H6NotificationProtocolException>(() => Parse("field.task.assigned.v1", Event("ASSIGNED") with { SourceKind = "1" }));
    [Fact]
    public void DuplicateSemanticPropertiesCannotUseLastKeyWins()
    {
        var value = Event("ASSIGNED"); var json = JsonSerializer.Serialize(value, Json);
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(value.EventId, "field.task.assigned.v1", At,
            json[..^1] + ",\"SourceKind\":\"FieldTask\"}"));
    }
    [Fact]
    public void FastTrackConfirmationInformsSupervisorAndDoesNotRequestNormalApproval()
    {
        var value = Event("FAST_TRACK_CONFIRMED") with { SourceKind = "RepairWork", SourceRevisionId = Guid.NewGuid() };
        var plan = Parse("repair.work.fast_track_confirmed.v1", value);
        Assert.Equal(NotificationEventKind.FastTrackConfirmedInformation, plan.Envelope!.Kind);
        Assert.Equal(NotificationRecipientStrategy.Supervisor, plan.Envelope.RecipientStrategy);
    }
}
