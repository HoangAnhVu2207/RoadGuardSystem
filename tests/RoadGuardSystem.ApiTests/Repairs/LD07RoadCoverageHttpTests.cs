using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Files;
using Xunit.Abstractions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Repairs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LD07RoadCoverageHttpTests(AuthenticationSqlServerFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task ProductionDiRequiresSupervisorExactSourceScopeVersionAndCurrentAuthorityBeforeReplay()
    {
        var pm = await fixture.CreateUserAsync("ld07-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await fixture.CreateUserAsync("ld07-sup-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var db = fixture.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        // Controlled TEST_ONLY geometry/obligation isolates the actual production mapping endpoint.
        // The SQL acceptance separately produces the real Report -> repair binding -> eligibility chain.
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "TEST_ONLY mapping HTTP", null, null, null, null, now);
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = supervisor.Id,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-2)
        };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "TEST_ONLY road");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry.CreateLineString([new(0, 0), new(10, 0)]), now, "TEST_ONLY; no real CRS claim");
        var type = DefectType.Create("LD07" + Guid.NewGuid().ToString("N"), "TEST_ONLY");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null, DefectSeverity.Low, DefectStatus.Open,
            geometry.CreatePoint(new Coordinate(5, 0)), now);
        db.AddRange(project, member, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, DateOnly.FromDateTime(now.UtcDateTime).AddDays(-2)), road, route, type, defect);
        await db.SaveChangesAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), road.Id, "TEST_ONLY_FRAME_V1", "TEST_ONLY", 1, 2, 0, 1));
        db.Add(RepairPackage.Create(Guid.NewGuid(), project.Id, defect.Id, [obligation])); await db.SaveChangesAsync();
        var file = StoredFile.Create(Guid.NewGuid(), "TEST_ONLY/ld07-http/" + Guid.NewGuid(), "basis.pdf", "application/pdf", 4, new string('f', 64), supervisor.Id, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, supervisor.Id, file.StorageUri, "TEST_ONLY_COVERAGE", file.MimeType, 4, file.Checksum, 8388608, now.AddHours(24));
        upload.StartUploading("TEST_ONLY", now); db.AddRange(file, upload, FileScope.Create(Guid.NewGuid(), file.Id, project.Id, project.Id, supervisor.Id, "TEST_ONLY_COVERAGE", now));
        await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var handover = HandoverDocument.Create(Guid.NewGuid(), project.Id, "TEST_ONLY-" + Guid.NewGuid(), DateOnly.FromDateTime(now.UtcDateTime).AddDays(-2), supervisor.Id, null, "TEST_ONLY");
        db.Add(handover); await db.SaveChangesAsync();
        var other = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Other project", null, null, null, null, now);
        db.Add(other);
        await db.SaveChangesAsync();
        var data = "%PDF-test"u8.ToArray();
        var checksum = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>();
            services.AddSingleton<IUploadObjectStorage>(new DocumentStorage(data.Length, checksum));
        });
        using var client = factory.CreateClient(); using var pmClient = factory.CreateClient();
        await Login(client, supervisor.Email!); await Login(pmClient, pm.Email!);
        using var web = await WebCookieTestClient.Login(factory, supervisor.Email!);
        await db.ProjectMembers.Where(row => row.UserId == supervisor.Id).ExecuteDeleteAsync();
        var path = $"/api/v1/projects/{project.Id}/road-coverage";
        var attachPath = $"/api/v1/projects/{project.Id}/handover-documents";
        using var inventory = await web.GetAsync(attachPath);
        Assert.Equal(HttpStatusCode.OK, inventory.StatusCode);
        Assert.True(inventory.Headers.CacheControl!.NoStore);
        var existingDocument = Assert.Single((await inventory.Content.ReadFromJsonAsync<HandoverDocumentsReadFact>())!.Items);
        Assert.Equal(handover.Id, existingDocument.Id);
        Assert.Null(existingDocument.FileId);
        Assert.True(existingDocument.CanAttachFile);
        Assert.False(existingDocument.IsMapped);
        Assert.Equal(Convert.ToBase64String(handover.RowVersion), existingDocument.Version);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(attachPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await pmClient.GetAsync(attachPath)).StatusCode);
        using (var anonymous = factory.CreateClient()) Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(attachPath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await web.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/handover-documents")).StatusCode);
        using var emptyInventory = await web.GetAsync($"/api/v1/projects/{other.Id}/handover-documents");
        Assert.Equal(HttpStatusCode.OK, emptyInventory.StatusCode);
        Assert.Empty((await emptyInventory.Content.ReadFromJsonAsync<HandoverDocumentsReadFact>())!.Items);
        var secondDocument = HandoverDocument.Create(Guid.NewGuid(), project.Id, "SECOND", handover.HandoverDate, supervisor.Id, null, null);
        db.Add(secondDocument); await db.SaveChangesAsync();
        using var multipleInventory = await web.GetAsync(attachPath);
        var documents = (await multipleInventory.Content.ReadFromJsonAsync<HandoverDocumentsReadFact>())!;
        Assert.Equal(new[] { handover.Id, secondDocument.Id }.Select(id => new System.Data.SqlTypes.SqlGuid(id)).Order(),
            documents.Items.Select(row => new System.Data.SqlTypes.SqlGuid(row.Id)));
        output.WriteLine("HANDOVER_INVENTORY_HTTP " + await multipleInventory.Content.ReadAsStringAsync());
        using var noHandover = await web.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, noHandover.StatusCode);
        Assert.DoesNotContain((await noHandover.Content.ReadFromJsonAsync<RoadCoverageReadFact>())!.Sources, row => row.Kind == "HANDOVER");

        async Task<(Guid UploadId, Guid FileId)> OpenUpload(Guid projectId)
        {
            using var create = await Send(web, "/api/v1/uploads", new
            {
                purpose = "DOCUMENT",
                projectId,
                targetId = projectId,
                fileName = "handover.pdf",
                mediaType = "application/pdf",
                sizeBytes = data.Length,
                checksumSha256 = checksum
            }, Guid.NewGuid().ToString());
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var uploadBody = await create.Content.ReadFromJsonAsync<JsonElement>();
            return (uploadBody.GetProperty("id").GetGuid(), uploadBody.GetProperty("fileId").GetGuid());
        }

        async Task VerifyUpload(Guid uploadId)
        {
            var uploadPath = $"/api/v1/uploads/{uploadId}";
            using var parts = await Send(web, uploadPath + "/part-urls", new { partNumbers = new[] { 1 } }, Guid.NewGuid().ToString());
            Assert.Equal(HttpStatusCode.OK, parts.StatusCode);
            using var session = await web.GetAsync(uploadPath);
            using var complete = await Send(web, uploadPath + "/complete",
                new { parts = new[] { new { partNumber = 1, eTag = "fixture-1" } }, checksumSha256 = checksum },
                Guid.NewGuid().ToString(), session.Headers.ETag!.Tag);
            Assert.Equal(HttpStatusCode.Accepted, complete.StatusCode);
            using (var scope = factory.Services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<IUploadService>().ProcessOneVerificationAsync();
            using var verified = await web.GetAsync(uploadPath);
            Assert.Equal("VERIFIED", (await verified.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        }

        var uploadIds = await OpenUpload(project.Id);
        var attach = new { fileId = uploadIds.FileId, handoverDocumentId = existingDocument.Id };
        var version = noHandover.Headers.ETag!.Tag;
        using var pending = await Send(web, attachPath, attach, Guid.NewGuid().ToString(), version);
        Assert.Equal(HttpStatusCode.Conflict, pending.StatusCode);
        Assert.Equal("handover_file_not_ready", (await pending.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await VerifyUpload(uploadIds.UploadId);
        var outsideUpload = await OpenUpload(other.Id);
        await VerifyUpload(outsideUpload.UploadId);
        var noDocumentPath = $"/api/v1/projects/{other.Id}/handover-documents";
        using var otherCoverage = await web.GetAsync($"/api/v1/projects/{other.Id}/road-coverage");
        using var newDocument = await Send(web, noDocumentPath, new
        {
            fileId = outsideUpload.FileId,
            documentNo = "TEST-HANDOVER-NEW",
            handoverDate = DateOnly.FromDateTime(now.UtcDateTime)
        }, Guid.NewGuid().ToString(), otherCoverage.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.OK, newDocument.StatusCode);
        Assert.Contains((await newDocument.Content.ReadFromJsonAsync<RoadCoverageReadFact>())!.Sources,
            row => row.Kind == "HANDOVER" && row.FileId == outsideUpload.FileId);

        // A verified DOCUMENT is itself now visible as a maintenance source, so fetch a fresh aggregate ETag.
        using var beforeAttach = await web.GetAsync(path);
        var attachVersion = beforeAttach.Headers.ETag!.Tag;
        using var selectionRequired = await Send(web, attachPath, new { fileId = uploadIds.FileId }, Guid.NewGuid().ToString(), attachVersion);
        Assert.Equal(HttpStatusCode.Conflict, selectionRequired.StatusCode);
        Assert.Equal("handover_document_selection_required", (await selectionRequired.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        using var wrongPurpose = await Send(web, attachPath, new { fileId = file.Id, handoverDocumentId = existingDocument.Id }, Guid.NewGuid().ToString(), attachVersion);
        Assert.Equal(HttpStatusCode.Conflict, wrongPurpose.StatusCode);
        Assert.Equal("handover_file_not_ready", (await wrongPurpose.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(pmClient, attachPath, attach, Guid.NewGuid().ToString(), attachVersion)).StatusCode);
        using var wrongProject = await Send(web, attachPath, new { fileId = outsideUpload.FileId }, Guid.NewGuid().ToString(), attachVersion);
        Assert.Equal(HttpStatusCode.Conflict, wrongProject.StatusCode);
        Assert.Equal("handover_file_not_ready", (await wrongProject.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        web.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(web, attachPath, attach, Guid.NewGuid().ToString(), attachVersion)).StatusCode);
        await WebCookieTestClient.RefreshCsrf(web);
        using var staleAttach = await Send(web, attachPath, attach, Guid.NewGuid().ToString(), version);
        Assert.Equal(HttpStatusCode.Conflict, staleAttach.StatusCode);
        Assert.Equal("concurrency_conflict", (await staleAttach.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        var attachKey = Guid.NewGuid().ToString();
        using var attached = await Send(web, attachPath, attach, attachKey, attachVersion);
        Assert.Equal(HttpStatusCode.OK, attached.StatusCode);
        using var replay = await Send(web, attachPath, attach, attachKey, attachVersion);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(attached.Headers.ETag, replay.Headers.ETag);
        using var afterInventory = await web.GetAsync(attachPath);
        var afterDocuments = (await afterInventory.Content.ReadFromJsonAsync<HandoverDocumentsReadFact>())!.Items;
        Assert.Equal(uploadIds.FileId, afterDocuments.Single(row => row.Id == handover.Id).FileId);
        Assert.False(afterDocuments.Single(row => row.Id == handover.Id).CanAttachFile);
        Assert.Null(afterDocuments.Single(row => row.Id == secondDocument.Id).FileId);
        var initial = await web.GetAsync(path); Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        var view = (await initial.Content.ReadFromJsonAsync<RoadCoverageReadFact>())!;
        var handoverPin = view.Sources.Single(row => row.Kind == "HANDOVER" && row.Id == handover.Id);
        var sourcePin = view.Sources.Single(row => row.Kind == "MAINTENANCE_BASIS" && row.Id == file.Id);
        var input = new RoadCoverageInputDto(obligation.Id, Assert.Single(view.Scopes).ScopeHash, handover.Id, handoverPin.Version,
            sourcePin.Kind, file.Id, sourcePin.Version, now.AddMinutes(-1), now.AddDays(1), "TEST_ONLY", "TEST_ONLY exact source-linked HTTP confirmation");
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await client.PostAsJsonAsync(path, input)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(pmClient, path, input, Guid.NewGuid().ToString(), initial.Headers.ETag!.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/road-coverage")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, input with { HandoverVersion = new string('0', 64) }, Guid.NewGuid().ToString(), initial.Headers.ETag!.ToString())).StatusCode);
        var key = Guid.NewGuid().ToString();
        var confirmed = await Send(web, path, input, key, initial.Headers.ETag!.ToString()); Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
        var mapping = Assert.Single((await confirmed.Content.ReadFromJsonAsync<RoadCoverageReadFact>())!.History);
        Assert.Equal(sourcePin.Version, mapping.SourceVersion); Assert.Equal(handoverPin.Version, mapping.HandoverVersion); Assert.Equal("TEST_ONLY", mapping.Provenance);
        Assert.Equal(uploadIds.FileId, handoverPin.FileId);
        using var replace = await Send(web, attachPath, attach, Guid.NewGuid().ToString(), confirmed.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.Conflict, replace.StatusCode);
        Assert.Equal("handover_document_already_linked", (await replace.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(1, await db.AuditLogs.CountAsync(row => row.EntityId == handover.Id && row.EventType == "handover_file_attached"));
        Assert.Equal(HttpStatusCode.OK, (await Send(client, path, input, key, initial.Headers.ETag.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, input with { Reason = "changed" }, key, initial.Headers.ETag.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, input, Guid.NewGuid().ToString(), initial.Headers.ETag.ToString())).StatusCode);
        await db.Users.Where(row => row.Id == supervisor.Id).ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, UserStatus.Suspended));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(client, path, input, key, initial.Headers.ETag.ToString())).StatusCode);
        Assert.Single(await db.Set<RoadCoverageMapping>().Where(row => row.ProjectId == project.Id).ToArrayAsync());
        output.WriteLine(JsonSerializer.Serialize(new
        {
            proof = "SWG-194",
            environment = "TEST_HOST_E2E",
            status = 201,
            actorId = supervisor.Id,
            role = "Supervisor",
            projectId = project.Id,
            obligationId = obligation.Id,
            handoverId = handover.Id,
            sourceFileId = file.Id,
            mappingId = mapping.Id,
            producer = "TEST_ONLY isolated SQL handover, verified upload and file scope prerequisite",
            request = input,
            persistedMappings = 1,
            provenance = "TEST_ONLY"
        }));
    }

    [Fact]
    public async Task CookieConfirmationRequiresCsrfBeforeAnyMappingOrReceipt()
    {
        var actor = await fixture.CreateUserAsync("ld07-cookie-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = (await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email = actor.Email, password = "Current1!" })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{Guid.NewGuid()}/road-coverage", new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("csrf_failed", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext(); Assert.False(await db.Set<RoadCoverageMapping>().AnyAsync(row => row.ActorId == actor.Id));
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }
    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static Task<HttpResponseMessage> Send(HttpClient client, string path, object input, string key, string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", key);
        if (version is not null) request.Headers.TryAddWithoutValidation("If-Match", version);
        return client.SendAsync(request);
    }

    private sealed class DocumentStorage(int bytes, string checksum) : IUploadObjectStorage
    {
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => Task.FromResult("fixture-upload");

        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers,
            DateTimeOffset expiresAt, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(n => new PresignedUploadPart(n, $"https://storage.test/{n}", expiresAt)).ToArray());

        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts,
            CancellationToken cancellationToken = default) => Task.FromResult(new UploadObjectVerification(bytes, checksum, "application/pdf"));

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
