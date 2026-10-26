using System.Security.Cryptography;
using RoadGuardSystem.Services.Processing;

namespace RoadGuardSystem.ApiTests.Infrastructure;

// Ephemeral, test-process-only custody; independent of the human JWT fixture.
internal static class TestAiServiceIdentity
{
    internal static readonly AiServiceTokenOptions Options = Create();
    internal static readonly AiCallbackTokens Tokens = new(Options, TimeProvider.System);
    private static AiServiceTokenOptions Create()
    {
        using var rsa = RSA.Create(2048);
        return new()
        {
            Enabled = true,
            Subject = "isolated-ai-test-service",
            ActiveKeyId = "ephemeral-ai",
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
            PublicKeys = new() { ["ephemeral-ai"] = rsa.ExportSubjectPublicKeyInfoPem() }
        };
    }
}
