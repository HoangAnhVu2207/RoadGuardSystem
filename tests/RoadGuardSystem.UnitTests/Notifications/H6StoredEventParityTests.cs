using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6StoredEventParityTests
{
    [Fact]
    public void InternalStoredEnvelopeHasExactPublicEventJsonAndHash()
    {
        var at = new DateTimeOffset(2026, 10, 12, 2, 0, 0, TimeSpan.Zero);
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var project = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var sourceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var origin = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var wire = new H6NotificationEventDto(1, id, "WEEKLY_PENDING", project, "ReviewObligation", sourceId,
            origin, at, id, null, at);
        var stored = new H6StoredEvent(1, id, "WEEKLY_PENDING", project, "ReviewObligation", sourceId,
            origin, at, id, null, at);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var wireJson = JsonSerializer.Serialize(wire, options);
        var storedJson = JsonSerializer.Serialize(stored, options);
        Assert.Equal(wireJson, storedJson);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(wireJson))),
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(storedJson))));
        Assert.Equal(stored, H6NotificationCatalog.Parse(id, "review.weekly_pending.v1", at, wireJson).Source);
    }
}
