using System.Text.Json;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6RepairCorrectionAuditCatalogTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Theory]
    [InlineData("UNREPAIRED")]
    [InlineData("REPORTED_AWAITING_REVIEW")]
    [InlineData("CONFIRMED")]
    public void ActualCorrectionWireIsFiniteAuditOnlyWithoutNotificationFanout(string result)
    {
        var source = Source(result);
        var plan = H6NotificationCatalog.Parse(source.EventId, "repair.decision.corrected.v1", source.OccurredAtUtc, JsonSerializer.Serialize(source, Json));
        Assert.True(plan.AuditOnly); Assert.Null(plan.Envelope);
        Assert.Equal(source.SourceRevisionId, plan.Source.SourceRevisionId);
        Assert.Equal(new H6StoredCorrectionFacts(source.ObligationId, source.SupersedesDecisionId, result), plan.AuditFacts);
        Assert.Empty(plan.Title); Assert.Empty(plan.Body);
    }
    [Fact]
    public void CorrectionsWithTransportIdentityDifferentFromImmutableDecisionAreRejected()
    {
        var source = Source("CONFIRMED") with { OriginEventId = Guid.NewGuid() };
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(source.EventId, "repair.decision.corrected.v1", source.OccurredAtUtc, JsonSerializer.Serialize(source, Json)));
    }
    [Fact]
    public void CorrectionCannotSmuggleRecipientOrUnknownResultIntoAuditWire()
    {
        var source = Source("CONFIRMED");
        var payload = JsonSerializer.Serialize(source, Json);
        payload = payload[..^1] + ",\"responsibleUserId\":\"" + Guid.NewGuid() + "\"}";
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(source.EventId, "repair.decision.corrected.v1", source.OccurredAtUtc, payload));
        var unknown = source with { Result = "ACCEPTED" };
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(unknown.EventId, "repair.decision.corrected.v1", unknown.OccurredAtUtc, JsonSerializer.Serialize(unknown, Json)));
    }
    [Fact]
    public void OrdinaryEventsRetainStrictDecoderAndCannotCarryCorrectionFacts()
    {
        var source = new H6NotificationEventDto(1, Guid.NewGuid(), "ASSIGNED", Guid.NewGuid(), "FieldTask", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, ResponsibleUserId: Guid.NewGuid());
        var payload = JsonSerializer.Serialize(source, Json);
        payload = payload[..^1] + ",\"obligationId\":\"" + Guid.NewGuid() + "\"}";
        Assert.Throws<H6NotificationProtocolException>(() => H6NotificationCatalog.Parse(source.EventId, "field.task.assigned.v1", source.OccurredAtUtc, payload));
    }
    private static H6RepairCorrectionAuditDto Source(string result)
    {
        var decision = Guid.NewGuid();
        return new(1, decision, decision, Guid.NewGuid(), "CORRECTED", "RepairWork", Guid.NewGuid(), decision,
            DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), result);
    }
}
