using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.ApiTests.Offline;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H5OfflinePositiveHttpTests(AuthenticationSqlServerFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task BearerSignedSyncAndSupervisorGrantedImportCommitOriginalCrewEffects()
    {
        var pm = await fixture.CreateUserAsync("h5-positive-pm-" + Guid.NewGuid().ToString("N"),
            "Current1!", UserRoleCode.ProjectManager);
        var crew = await fixture.CreateUserAsync("h5-positive-crew-" + Guid.NewGuid().ToString("N"),
            "Current1!", UserRoleCode.RepairCrew);
        var supervisor = await fixture.CreateUserAsync("h5-positive-supervisor-" + Guid.NewGuid().ToString("N"),
            "Current1!", UserRoleCode.Supervisor);
        var reporter = await fixture.CreateUserAsync("h5-positive-reporter-" + Guid.NewGuid().ToString("N"),
            "Current1!", UserRoleCode.Reporter);
        var scope = await SeedAsync(pm.Id, crew.Id, supervisor.Id, reporter.Id);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new("https://localhost") });
        var offline = $"/api/v1/projects/{scope.Project}/offline";
        var field = $"/api/v1/projects/{scope.Project}/field-inspection-tasks";

        await LoginAsync(client, pm.UserName!);
        var directTask = await CreateAcceptedTaskAsync(client, field, scope, pm.UserName!, crew.UserName!, crew.Id);
        await LoginAsync(client, crew.UserName!);
        var sourceDeviceId = Guid.NewGuid();
        using var sourceKeys = OfflineDeviceKeys.Generate(crew.Id, sourceDeviceId.ToString("D"));
        var sourceRegister = await PostAsync(client, offline + "/devices",
            new OfflineDeviceRegisterInput(sourceDeviceId, sourceKeys.PublicKeys.EncryptionPublicKey,
                sourceKeys.PublicKeys.SigningPublicKey));
        Assert.Equal(HttpStatusCode.Created, sourceRegister.StatusCode);
        var sourceRegistration = (await sourceRegister.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var firstSnapshot = await SnapshotAsync(client, offline, sourceRegistration, directTask);
        var first = Operation(crew.Id, sourceDeviceId, firstSnapshot, directTask);
        var firstManifest = OfflineWorkflowEngine.CanonicalManifest(scope.Project, Guid.NewGuid(),
            sourceRegistration, [OfflineWorkflowEngine.Describe(first)]);
        using var firstManifestDoc = JsonDocument.Parse(firstManifest);
        var firstBatchId = firstManifestDoc.RootElement.GetProperty("sourceBatchId").GetGuid();
        var firstSignature = OfflinePackageAuthentication.SignClaim(firstManifest, sourceKeys);
        var sync = new OfflineSignedBatchInput(firstBatchId, sourceRegistration, [first], firstSignature);
        var synced = await PostAsync(client, offline + "/sync", sync);
        Assert.Equal(HttpStatusCode.OK, synced.StatusCode);
        Assert.True((await synced.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items")[0]
            .GetProperty("durableAcknowledgment").GetBoolean());

        await LoginAsync(client, pm.UserName!);
        var importTask = await CreateAcceptedTaskAsync(client, field, scope, pm.UserName!, crew.UserName!, crew.Id);
        await LoginAsync(client, pm.UserName!);
        var recipientDeviceId = Guid.NewGuid();
        using var recipientKeys = OfflineDeviceKeys.Generate(pm.Id, recipientDeviceId.ToString("D"));
        var recipientRegister = await PostAsync(client, offline + "/devices",
            new OfflineDeviceRegisterInput(recipientDeviceId, recipientKeys.PublicKeys.EncryptionPublicKey,
                recipientKeys.PublicKeys.SigningPublicKey));
        Assert.Equal(HttpStatusCode.Created, recipientRegister.StatusCode);
        var recipientRegistration = (await recipientRegister.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await LoginAsync(client, crew.UserName!);
        var secondSnapshot = await SnapshotAsync(client, offline, sourceRegistration, importTask);
        var second = Operation(crew.Id, sourceDeviceId, secondSnapshot, importTask);
        var descriptor = OfflineWorkflowEngine.Describe(second);
        var sourceBatchId = Guid.NewGuid();
        var manifest = OfflineWorkflowEngine.CanonicalManifest(scope.Project, sourceBatchId,
            sourceRegistration, [descriptor]);
        var signature = OfflinePackageAuthentication.SignClaim(manifest, sourceKeys);
        var attached = OfflineWorkflowEngine.CanonicalAttachedPayload(sourceBatchId, sourceRegistration,
            [second], signature);
        var hash = Convert.ToHexString(SHA256.HashData(attached)).ToLowerInvariant();
        var packageId = Guid.NewGuid();
        var header = OfflinePackageHeader.Create(packageId, scope.Project, crew.Id,
            sourceDeviceId.ToString("D"), [second.OriginId], hash);
        var ciphertext = OfflineHandoverCrypto.Seal(header, attached, sourceKeys, [recipientKeys.PublicKeys]);
        var exported = await PostAsync(client, offline + "/packages/export",
            new OfflinePackageExportInput(H5EncryptedPackageDto.FromDomain(ciphertext),
                new(sourceBatchId, sourceRegistration, [descriptor], signature)));
        Assert.Equal(HttpStatusCode.Created, exported.StatusCode);
        await using (var inspect = fixture.CreateDbContext())
        {
            var stored = await inspect.Set<OfflineEncryptedPackageRecord>().AsNoTracking()
                .SingleAsync(row => row.Id == packageId);
            var recipient = await inspect.Set<OfflineDeviceRegistration>().AsNoTracking()
                .SingleAsync(row => row.Id == recipientRegistration);
            Assert.Equal(crew.Id, stored.OriginalActorId);
            Assert.Equal(sourceRegistration, stored.SourceDeviceRegistrationId);
            Assert.Equal(pm.Id, recipient.ActorId);
            Assert.Equal(recipientDeviceId, recipient.DeviceId);
            Assert.Equal(recipientKeys.PublicKeys.EncryptionPublicKey, recipient.EncryptionPublicKey);
            var storedCipher = JsonSerializer.Deserialize<OfflineEncryptedPackage>(stored.CipherPackageJson, Json)!;
            var fingerprint = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(recipient.EncryptionPublicKey)))
                .ToLowerInvariant();
            Assert.Contains(storedCipher.Recipients, wrap => wrap.ActorId == recipient.ActorId &&
                wrap.DeviceId == recipient.DeviceId.ToString("D") && wrap.RecipientKeyFingerprint == fingerprint);
        }
        await LoginAsync(client, supervisor.UserName!);
        var tokenClaims = new JwtSecurityTokenHandler().ReadJwtToken(
            client.DefaultRequestHeaders.Authorization!.Parameter!);
        Assert.Equal(supervisor.Id.ToString("D"), tokenClaims.Claims.Single(claim => claim.Type == "sub").Value);
        var granted = await PostAsync(client, offline + "/grants",
            new OfflineHandoverGrantInput(packageId, recipientRegistration,
                [new(second.OriginId, second.Kind, second.CorePayloadHash, descriptor.EnvelopeHash,
                    second.TaskId, second.AssignmentId, second.SnapshotId)], "recover signed data"));
        Assert.True(granted.StatusCode == HttpStatusCode.Created,
            await granted.Content.ReadAsStringAsync());
        var grantId = (await granted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await LoginAsync(client, pm.UserName!);
        var importBatchId = Guid.NewGuid();
        var recipientClaim = OfflineRecipientEndorsement.CanonicalClaim(new(scope.Project, packageId,
            grantId, importBatchId, recipientRegistration, hash));
        var recipientSignature = OfflinePackageAuthentication.SignClaim(recipientClaim, recipientKeys);
        var import = new OfflinePackageImportInput(importBatchId, packageId, grantId, recipientRegistration,
            [second], signature, recipientSignature);
        var imported = await PostAsync(client, offline + "/import", import);
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        var item = (await imported.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items")[0];
        Assert.True(item.GetProperty("durableAcknowledgment").GetBoolean());
        Assert.Equal(second.OriginId, item.GetProperty("effectId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, offline + "/import", import)).StatusCode);
        await using var verify = fixture.CreateDbContext();
        Assert.Equal(1, await verify.Set<FieldTaskStartOrigin>().CountAsync(row => row.TaskId == directTask.TaskId));
        Assert.Equal(1, await verify.Set<FieldTaskStartOrigin>().CountAsync(row => row.TaskId == importTask.TaskId));
        Assert.Equal(1, await verify.Set<OfflineOperationResult>().CountAsync(row =>
            row.OriginId == second.OriginId && row.DurableAck));
        Assert.Equal(crew.Id, (await verify.Set<FieldTaskStartOrigin>().SingleAsync(row =>
            row.TaskId == importTask.TaskId)).OriginalActorId);
    }

    private async Task<(Guid Project, Guid Route, Guid Set, Guid Defect, string Version)> SeedAsync(
        Guid pm, Guid crew, Guid supervisor, Guid reporter)
    {
        await using var db = fixture.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "H5 HTTP", null, null,
            null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "HTTP fixture");
        var line = new GeometryFactory(new PrecisionModel(), 32648)
            .CreateLineString([new(0, 0), new(20, 0)]);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, line, now, "HTTP fixture");
        var set = RoadSegmentSet.Create(Guid.NewGuid(), route.Id);
        var segment = RoadSegment.Create(Guid.NewGuid(), set.Id, route.Id, 1);
        segment.SetGeometry(0, 20, 0, line);
        var type = DefectType.Create("H" + Guid.NewGuid().ToString("N"), "HTTP offline source");
        db.AddRange(project, road, route, set, segment, type,
            ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm, new(2000, 1, 1)),
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = crew,
                RoleCode = UserRoleCode.RepairCrew, Status = ProjectMemberStatus.Active, ValidFrom = new(2000, 1, 1) },
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = supervisor,
                RoleCode = UserRoleCode.Supervisor, Status = ProjectMemberStatus.Active, ValidFrom = new(2000, 1, 1) });
        await db.SaveChangesAsync();
        var file = StoredFile.Create(Guid.NewGuid(), "reporter/h5-" + Guid.NewGuid().ToString("N"),
            "source.jpg", "image/jpeg", 4, new string('b', 64), reporter, now, null);
        db.AddRange(file, FileScope.CreatePrivate(Guid.NewGuid(), file.Id, reporter, now));
        await db.SaveChangesAsync();
        var report = Report.Create(Guid.NewGuid(), reporter, "reporter source", now,
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), file.Id, "fixture-version", reporter)]);
        var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, now);
        incident.Triage(project.Id, CaseVerificationMethod.ExistingEvidence, "source", now);
        db.AddRange(report, incident);
        db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = incident.Id,
            ReportId = report.Id, StartedAt = now });
        await db.SaveChangesAsync();
        var source = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report,
            report.Id, "fixture-source"), project.Id, "fixture-geometry");
        var accepted = await new CandidateDecisionRepository(db).SaveAcceptedAsync(pm, source,
            CandidateDecisionKind.KeepNew,
            CandidateClassification.Create(route.Id, type.Code, null, DefectSeverity.Low, null),
            null, null, null, "PM keep-new", null, default);
        var version = await db.Defects.Where(row => row.Id == accepted.DefectId)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync();
        return (project.Id, route.Id, set.Id, accepted.DefectId!.Value,
            Convert.ToBase64String(version));
    }

    private async Task<(Guid TaskId, Guid AssignmentId, string Version)> CreateAcceptedTaskAsync(
        HttpClient client, string field, (Guid Project, Guid Route, Guid Set, Guid Defect, string Version) scope,
        string pm, string crew, Guid crewId)
    {
        await LoginAsync(client, pm);
        await using var db = fixture.CreateDbContext();
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == scope.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var created = await PostAsync(client, field, new FieldTaskCreateInput(scope.Defect, defectVersion,
            null, "REPORTER", scope.Route, scope.Set, null, null, "PRE_MEASUREMENT", 1,
            "{}", "offline capture", crewId, DateTimeOffset.UtcNow.AddDays(1)));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var taskId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await LoginAsync(client, crew);
        var task = await client.GetFromJsonAsync<JsonElement>(field + "/" + taskId);
        var accepted = await PostAsync(client, field + "/" + taskId + "/accept",
            new FieldTaskActionInput("accept"), task.GetProperty("version").GetString());
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        var current = await client.GetFromJsonAsync<JsonElement>(field + "/" + taskId);
        return (taskId, current.GetProperty("assignmentId").GetGuid(),
            current.GetProperty("version").GetString()!);
    }

    private async Task<(Guid SnapshotId, Guid TaskId, Guid AssignmentId, string Version)> SnapshotAsync(HttpClient client,
        string offline, Guid registration, (Guid TaskId, Guid AssignmentId, string Version) task)
    {
        var response = await PostAsync(client, offline + "/snapshots",
            new OfflineSnapshotInput(registration, task.TaskId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var row = await response.Content.ReadFromJsonAsync<JsonElement>();
        await using var db = fixture.CreateDbContext();
        var snapshot = await db.Set<OfflineTaskSnapshot>().SingleAsync(value =>
            value.Id == row.GetProperty("id").GetGuid());
        return (snapshot.Id, task.TaskId, snapshot.AssignmentId, snapshot.TaskVersion);
    }

    private static OfflineOperationInput Operation(Guid originalActor, Guid sourceDevice,
        (Guid SnapshotId, Guid TaskId, Guid AssignmentId, string Version) snapshot,
        (Guid TaskId, Guid AssignmentId, string Version) task)
    {
        var origin = Guid.NewGuid();
        var operation = new OfflineOperationInput(1, origin, origin, "FIELD_START", originalActor,
            sourceDevice, snapshot.SnapshotId, task.TaskId, snapshot.AssignmentId, snapshot.Version,
            new string('0', 64), [], null, new FieldStartInput(origin, DateTimeOffset.UtcNow, sourceDevice));
        return operation with { CorePayloadHash = OfflineWorkflowEngine.FieldCoreHash(operation) };
    }

    private static async Task LoginAsync(HttpClient client, string userName)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(userName), password = "Current1!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string route, object body,
        string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", "h5-" + Guid.NewGuid().ToString("N"));
        if (version is not null) request.Headers.TryAddWithoutValidation("If-Match", "\"" + version + "\"");
        return client.SendAsync(request);
    }
}
