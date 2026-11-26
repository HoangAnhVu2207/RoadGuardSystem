using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Services.Offline;

var input = JsonDocument.Parse(await Console.In.ReadToEndAsync()).RootElement;
var project = input.GetProperty("project").GetGuid();
var taskId = input.GetProperty("task").GetGuid();
var offline = $"projects/{project}/offline";
var api = new Uri(input.GetProperty("api").GetString()!.TrimEnd('/') + "/api/v1/");
if (!api.IsLoopback || api.Scheme != Uri.UriSchemeHttp)
    throw new InvalidOperationException("The local offline fixture requires a loopback HTTP endpoint.");
using var client = new HttpClient(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false })
{
    BaseAddress = api,
    Timeout = TimeSpan.FromSeconds(30)
};
async Task<JsonElement> Call(string label, string path, HttpMethod method, object? payload = null, string? token = null)
{
    using var request = new HttpRequestMessage(method, path);
    if (payload is not null) request.Content = JsonContent.Create(payload);
    if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (method == HttpMethod.Post) request.Headers.Add("Idempotency-Key", $"demo-offline-{taskId:N}-{label}");
    using var response = await client.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? code = body.TryGetProperty("code", out var candidate) ? candidate.GetString() : null;
        throw new InvalidOperationException($"{label} returned {(int)response.StatusCode} {code}");
    }
    return await response.Content.ReadFromJsonAsync<JsonElement>();
}

async Task<(Guid Actor, string Token)> Login(string role)
{
    var result = await Call("login-" + role, "auth/android/login", HttpMethod.Post, input.GetProperty(role));
    return (result.GetProperty("user").GetProperty("id").GetGuid(), result.GetProperty("accessToken").GetString()!);
}

async Task<Guid> Register(OfflineDeviceKeys keys, Guid device, string token)
{
    var response = await Call("register-" + device.ToString("N"), offline + "/devices", HttpMethod.Post,
        new OfflineDeviceRegisterInput(device, keys.PublicKeys.EncryptionPublicKey, keys.PublicKeys.SigningPublicKey), token);
    return response.GetProperty("id").GetGuid();
}

try
{
    var crew = await Login("crew");
    var pm = await Login("pm");
    var supervisor = await Login("supervisor");
    var task = await Call("read-task", $"projects/{project}/field-inspection-tasks/{taskId}", HttpMethod.Get, token: crew.Token);
    if (input.TryGetProperty("previous", out var previous))
    {
        if (previous.GetProperty("project").GetGuid() != project || previous.GetProperty("task").GetGuid() != taskId)
            throw new InvalidOperationException("Offline receipt belongs to a different fixture");
        foreach (var collection in new[] { "devices", "packages", "grants" })
        {
            var page = await Call(collection, offline + "/" + collection, HttpMethod.Get, token: pm.Token);
            if (!page.GetProperty("items").EnumerateArray().Any(row => row.GetProperty("id").GetGuid() == previous.GetProperty(collection).GetGuid()))
                throw new InvalidOperationException("Offline readback missing " + collection);
        }
        Console.WriteLine(previous.GetRawText());
        return;
    }
    var device = Guid.NewGuid();
    var recipientDevice = Guid.NewGuid();
    using var sourceKeys = OfflineDeviceKeys.Generate(crew.Actor, device.ToString("D"));
    using var recipientKeys = OfflineDeviceKeys.Generate(pm.Actor, recipientDevice.ToString("D"));
    var registration = await Register(sourceKeys, device, crew.Token);
    var recipient = await Register(recipientKeys, recipientDevice, pm.Token);
    var snapshot = await Call("snapshot", offline + "/snapshots", HttpMethod.Post,
        new OfflineSnapshotInput(registration, taskId), crew.Token);
    var snapshotId = snapshot.GetProperty("id").GetGuid();
    var origin = Guid.NewGuid();
    var operation = new OfflineOperationInput(1, origin, origin, "FIELD_START", crew.Actor, device,
        snapshotId, taskId, task.GetProperty("assignmentId").GetGuid(), task.GetProperty("version").GetString()!,
        new string('0', 64), [], null, new FieldStartInput(origin, DateTimeOffset.UtcNow, device));
    operation = operation with { CorePayloadHash = OfflineWorkflowEngine.FieldCoreHash(operation) };
    var sourceBatch = Guid.NewGuid();
    var descriptor = OfflineWorkflowEngine.Describe(operation);
    var manifest = OfflineWorkflowEngine.CanonicalManifest(project, sourceBatch, registration, [descriptor]);
    var signature = OfflinePackageAuthentication.SignClaim(manifest, sourceKeys);
    var attached = OfflineWorkflowEngine.CanonicalAttachedPayload(sourceBatch, registration, [operation], signature);
    var digest = Convert.ToHexString(SHA256.HashData(attached)).ToLowerInvariant();
    var packageId = Guid.NewGuid();
    var header = OfflinePackageHeader.Create(packageId, project, crew.Actor, device.ToString("D"), [origin], digest);
    var encrypted = OfflineHandoverCrypto.Seal(header, attached, sourceKeys, [recipientKeys.PublicKeys]);
    await Call("export", offline + "/packages/export", HttpMethod.Post,
        new OfflinePackageExportInput(H5EncryptedPackageDto.FromDomain(encrypted),
            new(sourceBatch, registration, [descriptor], signature)), crew.Token);
    var grant = await Call("grant", offline + "/grants", HttpMethod.Post,
        new OfflineHandoverGrantInput(packageId, recipient,
            [new(origin, operation.Kind, operation.CorePayloadHash, descriptor.EnvelopeHash, taskId,
                operation.AssignmentId, snapshotId)], "DEMO signed offline handover"), supervisor.Token);
    var grantId = grant.GetProperty("id").GetGuid();
    var importBatch = Guid.NewGuid();
    var claim = OfflineRecipientEndorsement.CanonicalClaim(new(project, packageId, grantId, importBatch, recipient, digest));
    var recipientSignature = OfflinePackageAuthentication.SignClaim(claim, recipientKeys);
    var import = new OfflinePackageImportInput(importBatch, packageId, grantId, recipient, [operation], signature, recipientSignature);
    var imported = await Call("import", offline + "/import", HttpMethod.Post, import, pm.Token);
    if (!imported.GetProperty("items")[0].GetProperty("durableAcknowledgment").GetBoolean())
        throw new InvalidOperationException("Offline import was not durably acknowledged");
    var replay = await Call("import", offline + "/import", HttpMethod.Post, import, pm.Token);
    if (!replay.GetProperty("items")[0].GetProperty("durableAcknowledgment").GetBoolean())
        throw new InvalidOperationException("Replayed import did not return its durable acknowledgment");
    foreach (var (collection, id) in new[] { ("devices", recipient), ("packages", packageId), ("grants", grantId) })
    {
        var page = await Call(collection, offline + "/" + collection, HttpMethod.Get, token: pm.Token);
        if (!page.GetProperty("items").EnumerateArray().Any(item => item.GetProperty("id").GetGuid() == id))
            throw new InvalidOperationException($"Offline {collection} read-back missed the seeded row");
    }
    Console.WriteLine(JsonSerializer.Serialize(new {
        status = "PASS", project, task = taskId, devices = recipient, packages = packageId,
        grants = grantId, durableImport = true, replay = true
    }));
}
catch (Exception error)
{
    Console.Error.WriteLine(error is InvalidOperationException ? error.Message : error.GetType().Name);
    Environment.ExitCode = 1;
}
