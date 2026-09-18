using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

[Trait("TaskId", "P2-02")]
public sealed class P202ValidationAndRedactionTests
{
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
