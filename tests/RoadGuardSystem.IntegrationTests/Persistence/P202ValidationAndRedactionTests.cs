using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

[Trait("TaskId", "P2-02")]
public sealed class P202ValidationAndRedactionTests : IClassFixture<P202SqlServerFixture>
{
    private readonly P202SqlServerFixture _fixture;

    public P202ValidationAndRedactionTests(P202SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-02 Negative F-01: public persistence path redacts nested case-variant sensitive keys")]
    public async Task PublicPersistencePath_RedactsSensitiveJsonBeforeSqlCommit()
    {
        var sentinel = $"P2_02_F01_{Guid.NewGuid():N}";
        var auditId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        var rawJson = $$"""
            {
              "safe": "visible",
              "nested": [
                { "ToKeN": "{{sentinel}}" },
                { "AUTHORIZATION": "{{sentinel}}" },
                { "connectionSTRING": "{{sentinel}}" }
              ]
            }
            """;

        await using (var context = _fixture.CreateDbContext())
        {
            context.AuditLogs.Add(AuditLog.Create(
                auditId,
                null,
                DateTimeOffset.UtcNow,
                "p2_02.f01_regression",
                "P202TransactionProbe",
                Guid.NewGuid(),
                rawJson,
                rawJson,
                null,
                "integration_test",
                Guid.NewGuid()));
            context.OutboxMessages.Add(OutboxMessage.Create(
                outboxId,
                "p2_02.f01_regression",
                DateTimeOffset.UtcNow,
                Guid.NewGuid(),
                rawJson));

            await context.SaveChangesAsync();
        }

        await using var verification = _fixture.CreateDbContext();
        var audit = await verification.AuditLogs.AsNoTracking().SingleAsync(row => row.Id == auditId);
        var outbox = await verification.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == outboxId);

        audit.BeforeSnapshot.Should().NotContain(sentinel).And.Contain("[REDACTED]");
        audit.AfterSnapshot.Should().NotContain(sentinel).And.Contain("[REDACTED]");
        outbox.PayloadJson.Should().NotContain(sentinel).And.Contain("[REDACTED]");
    }

    [Fact(DisplayName = "P2-02 Negative F-01: EF property mutation cannot bypass persistence redaction")]
    public async Task EfPropertyMutation_CannotBypassSensitiveJsonRedaction()
    {
        var sentinel = $"P2_02_F01_BYPASS_{Guid.NewGuid():N}";
        var auditId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        var rawJson = $"{{\"nested\":[{{\"sEcReT\":\"{sentinel}\"}},{{\"COOKIE\":\"{sentinel}\"}},{{\"PaSsWoRd\":\"{sentinel}\"}}]}}";

        await using (var context = _fixture.CreateDbContext())
        {
            var audit = AuditLog.Create(
                auditId,
                null,
                DateTimeOffset.UtcNow,
                "p2_02.f01_bypass_regression",
                "P202TransactionProbe",
                Guid.NewGuid(),
                null,
                "{\"safe\":\"initial\"}",
                null,
                "integration_test",
                Guid.NewGuid());
            var outbox = OutboxMessage.Create(
                outboxId,
                "p2_02.f01_bypass_regression",
                DateTimeOffset.UtcNow,
                Guid.NewGuid(),
                "{\"safe\":\"initial\"}");
            context.AuditLogs.Add(audit);
            context.OutboxMessages.Add(outbox);

            context.Entry(audit).Property(row => row.AfterSnapshot).CurrentValue = rawJson;
            context.Entry(outbox).Property(row => row.PayloadJson).CurrentValue = rawJson;
            await context.SaveChangesAsync();
        }

        await using var verification = _fixture.CreateDbContext();
        var persistedAudit = await verification.AuditLogs.AsNoTracking().SingleAsync(row => row.Id == auditId);
        var persistedOutbox = await verification.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == outboxId);

        persistedAudit.AfterSnapshot.Should().NotContain(sentinel).And.Contain("[REDACTED]");
        persistedOutbox.PayloadJson.Should().NotContain(sentinel).And.Contain("[REDACTED]");
    }

    [Fact(DisplayName = "P2-02 Negative: audit snapshot redacts sensitive keys recursively and applies allow-list")]
    public void AuditSnapshot_RedactsSensitiveKeysRecursively()
    {
        var raw = """
            {
              "safe": "visible",
              "unexpected": "remove-me",
              "Password": "pw-value",
              "nested": {
                "TOKEN": "token-value",
                "safe": "nested-visible",
                "items": [
                  { "authorization": "bearer-value", "safe": "array-visible" },
                  { "connectionString": "connection-value", "cookie": "cookie-value" }
                ]
              }
            }
            """;

        var sanitized = BuildSnapshot(
            raw,
            ["safe", "Password", "nested", "TOKEN", "items", "authorization", "connectionString", "cookie"],
            4096);

        sanitized.Should().NotContainAny(
            "pw-value",
            "token-value",
            "bearer-value",
            "connection-value",
            "cookie-value",
            "remove-me");
        sanitized.Should().Contain("visible").And.Contain("nested-visible").And.Contain("array-visible");

        using var document = JsonDocument.Parse(sanitized);
        document.RootElement.GetProperty("Password").GetString().Should().Be("[REDACTED]");
        document.RootElement.GetProperty("nested").GetProperty("TOKEN").GetString().Should().Be("[REDACTED]");
    }

    [Fact(DisplayName = "P2-02 Negative: malformed audit snapshot JSON is rejected before persistence")]
    public void AuditSnapshot_RejectsMalformedJson()
    {
        var action = () => BuildSnapshot("{ malformed", ["safe"], 4096);

        action.Should().Throw<JsonException>();
    }

    [Fact(DisplayName = "P2-02 Edge: oversized audit snapshot is rejected using caller-provided limit")]
    public void AuditSnapshot_RejectsOversizedInput()
    {
        var action = () => BuildSnapshot("{\"safe\":\"value-that-is-too-long\"}", ["safe"], 16);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*maximum*16*");
    }

    [Fact(DisplayName = "P2-02 Negative: audit validation exception does not disclose sensitive input")]
    public void AuditSnapshot_ExceptionDoesNotDiscloseSensitiveInput()
    {
        var sensitiveValue = $"secret-{Guid.NewGuid():N}";
        var action = () => BuildSnapshot(
            $"{{\"token\":\"{sensitiveValue}\"}}",
            ["token"],
            8);

        var exception = action.Should().Throw<ArgumentException>().Which;
        exception.ToString().Should().NotContain(sensitiveValue);
    }

    [Fact(DisplayName = "P2-02 Negative: JSON scalar is rejected by application schema validation")]
    public void JsonScalar_IsRejectedByApplicationValidation()
    {
        var auditBuilder = () => BuildSnapshot("\"scalar\"", ["safe"], 4096);
        var auditEntity = () => AuditLog.Create(
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            "p2_02.scalar",
            "P202TransactionProbe",
            Guid.NewGuid(),
            "\"scalar\"",
            null,
            null,
            "integration_test",
            Guid.NewGuid());
        var idempotencyEntity = () => IdempotencyRecord.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "survey.submit",
            "scalar-key",
            new string('a', 64),
            Guid.NewGuid(),
            "\"scalar\"",
            DateTimeOffset.UtcNow);
        var outboxEntity = () => OutboxMessage.Create(
            Guid.NewGuid(),
            "p2_02.scalar",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "\"scalar\"");

        auditBuilder.Should().Throw<ArgumentException>();
        auditEntity.Should().Throw<ArgumentException>();
        idempotencyEntity.Should().Throw<ArgumentException>();
        outboxEntity.Should().Throw<ArgumentException>();
    }

    private static string BuildSnapshot(string json, string[] allowList, int maxUtf8Bytes)
    {
        var type = P202ProductionContract.RequireRepositoryType("Auditing.AuditSnapshotBuilder");
        var method = P202ProductionContract.RequirePublicMethod(type, "Build", isStatic: true, parameterCount: 3);

        try
        {
            return (string)method.Invoke(null, [json, allowList, maxUtf8Bytes])!;
        }
        catch (Exception exception)
        {
            throw P202ProductionContract.UnwrapInvocation(exception);
        }
    }
}
