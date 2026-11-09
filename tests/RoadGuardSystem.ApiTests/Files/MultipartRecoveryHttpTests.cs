using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.ApiTests.Inspections;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Files;
using Xunit;

namespace RoadGuardSystem.ApiTests.Files;

[Collection(AuthenticationApiFixture.Name)]
[Trait("Package", "HUY-01")]
public sealed class MultipartRecoveryHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task LostAcknowledgement_NewHostRecoversSameKey_ConflictsAndAuthority_CompleteDownload()
    {
        var user = await sql.CreateUserAsync($"recover-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var other = await sql.CreateUserAsync($"wrong-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var remote = new RemotePhoto();
        Guid upload, file;
        var partKey = Guid.NewGuid().ToString();
        await using (var initial = Factory(remote, loseAck: true))
        {
            using var client = initial.CreateClient(new() { BaseAddress = new Uri("https://localhost") }); await LoginAsync(client, user.UserName!);
            using var create = await CreateAsync(client); create.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await create.Content.ReadFromJsonAsync<JsonElement>(); upload = body.GetProperty("id").GetGuid(); file = body.GetProperty("fileId").GetGuid();
            using var failed = await PartsAsync(client, upload, partKey); failed.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            (await failed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("upload_storage_unavailable");
        }
        await using var db = sql.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET MultipartNextCheckAt={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={upload}");
        await using var restarted = Factory(remote, loseAck: false);
        using var current = restarted.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(current, other.UserName!);
        (await PartsAsync(current, upload, partKey)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        await LoginAsync(current, user.UserName!);
        (await PartsAsync(current, upload, partKey)).StatusCode.Should().Be(HttpStatusCode.OK);
        using var secondCreate = await CreateAsync(current);
        var secondUpload = (await secondCreate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await PartsAsync(current, secondUpload, partKey)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PartsAsync(current, upload, partKey)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PartsAsync(current, upload, partKey, [1, 2])).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity); // invalid shape, never rewrites receipt
        var session = await current.GetAsync($"/api/v1/reporter-evidence/uploads/{upload}");
        using var complete = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/complete")
        { Content = JsonContent.Create(new { checksumSha256 = RemotePhoto.Hash, parts = new[] { new { partNumber = 1, eTag = "fixture" } } }) };
        complete.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); complete.Headers.TryAddWithoutValidation("If-Match", session.Headers.ETag!.ToString());
        (await current.SendAsync(complete)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        using (var cancel = await CancelAsync(current, upload)) cancel.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using (var scope = restarted.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<IUploadService>().ProcessOneVerificationAsync();
        using (var cancel = await CancelAsync(current, upload)) cancel.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var content = await current.GetAsync($"/api/v1/reporter-evidence/files/{file}/content"); content.StatusCode.Should().Be(HttpStatusCode.OK);
        Convert.ToHexString(SHA256.HashData(await content.Content.ReadAsByteArrayAsync())).ToLowerInvariant().Should().Be(RemotePhoto.Hash);
        remote.Initiations.Should().Be(1);
        (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == user.Id && r.Operation == "UploadPartUrlsIssued")).Should().Be(1);
        (await db.Files.CountAsync(f => f.Id == file)).Should().Be(1);
        (await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload)).Status.Should().Be(UploadSessionStatus.Verified);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status={(byte)UserStatus.Suspended} WHERE Id={user.Id}");
        (await PartsAsync(current, upload, partKey)).StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PendingReporterUploadCanBeCancelledOnlyByItsCurrentOwner()
    {
        var owner = await sql.CreateUserAsync($"cancel-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var other = await sql.CreateUserAsync($"other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var remote = new RemotePhoto();
        await using var factory = Factory(remote, false);
        using var client = factory.CreateClient();
        using var create = await CreateForOwnerAsync(client, owner.UserName!);
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var anonymous = factory.CreateClient(new() { HandleCookies = false });
        using var missingAuth = await CancelAsync(anonymous, id);
        missingAuth.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var outsider = factory.CreateClient();
        await LoginAsync(outsider, other.UserName!);
        using var forbidden = await CancelAsync(outsider, id);
        forbidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var cancelled = await CancelAsync(client, id);
        var original = await AssertCancelledAsync(cancelled, id);
        using var replay = await CancelAsync(client, id);
        (await AssertCancelledAsync(replay, id)).Should().Be(original);
        using var parts = await PartsAsync(client, id, Guid.NewGuid().ToString());
        parts.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var complete = CompleteRequest(id, create.Headers.ETag!.ToString());
        using var rejected = await client.SendAsync(complete);
        rejected.StatusCode.Should().Be(HttpStatusCode.Conflict);
        remote.Initiations.Should().Be(0);
    }

    [Fact]
    public async Task CancelledUploadingSessionRejectsPartReceiptAndRetriesCleanup()
    {
        var owner = await sql.CreateUserAsync($"cleanup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var remote = new RemotePhoto { FailNextAbort = true };
        await using var factory = Factory(remote, false);
        using var client = factory.CreateClient();
        using var create = await CreateForOwnerAsync(client, owner.UserName!);
        var body = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();
        var file = body.GetProperty("fileId").GetGuid();
        var key = Guid.NewGuid().ToString();
        using var issued = await PartsAsync(client, id, key);
        issued.StatusCode.Should().Be(HttpStatusCode.OK);
        using var session = await client.GetAsync($"/api/v1/reporter-evidence/uploads/{id}");
        using var cancel = await CancelAsync(client, id);
        var original = await AssertCancelledAsync(cancel, id);
        using var replayParts = await PartsAsync(client, id, key);
        replayParts.StatusCode.Should().Be(HttpStatusCode.Conflict);
        remote.Signatures.Should().Be(1);
        using var complete = CompleteRequest(id, session.Headers.ETag!.ToString());
        using var rejected = await client.SendAsync(complete);
        rejected.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await RecoverAsync(factory);
        remote.AbortAttempts.Should().Be(1);
        remote.Aborted.Should().BeFalse();
        await using (var db = sql.CreateDbContext())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET MultipartNextCheckAt={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={id}");
        await RecoverAsync(factory);
        remote.AbortAttempts.Should().Be(2);
        remote.Aborted.Should().BeTrue();
        remote.LastAbort.Should().Be(($"uploads/{file:N}", "fixture-id"));
        using var replay = await CancelAsync(client, id);
        (await AssertCancelledAsync(replay, id)).Should().Be(original);
    }

    [Fact]
    public async Task CancelAndCompleteAllowExactlyOneTransition()
    {
        var owner = await sql.CreateUserAsync($"race-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = Factory(new RemotePhoto(), false);
        using var client = factory.CreateClient();
        using var create = await CreateForOwnerAsync(client, owner.UserName!);
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var issued = await PartsAsync(client, id, Guid.NewGuid().ToString());
        issued.StatusCode.Should().Be(HttpStatusCode.OK);
        using var session = await client.GetAsync($"/api/v1/reporter-evidence/uploads/{id}");
        using var complete = CompleteRequest(id, session.Headers.ETag!.ToString());
        var cancelTask = CancelAsync(client, id);
        var completeTask = client.SendAsync(complete);
        await Task.WhenAll(cancelTask, completeTask);
        using var cancelled = await cancelTask;
        using var completed = await completeTask;
        await using var db = sql.CreateDbContext();
        var saved = await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == id);
        if (cancelled.StatusCode == HttpStatusCode.OK)
        {
            completed.StatusCode.Should().Be(HttpStatusCode.Conflict);
            saved.Status.Should().Be(UploadSessionStatus.Failed);
            saved.FailureCode.Should().Be("upload_cancelled");
        }
        else
        {
            cancelled.StatusCode.Should().Be(HttpStatusCode.Conflict);
            completed.StatusCode.Should().Be(HttpStatusCode.Accepted);
            saved.Status.Should().Be(UploadSessionStatus.Verifying);
        }
    }

    [Fact]
    public async Task CrewCancelRequiresCurrentOwnerProjectAndTaskWriteAccess()
    {
        var fixture = await SeedCrewTaskAsync();
        await using var factory = Factory(new RemotePhoto(), false);
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        using var pm = factory.CreateClient();
        using var anonymous = factory.CreateClient();
        await LoginAsync(owner, fixture.Crew);
        await LoginAsync(other, fixture.Other);
        await LoginAsync(pm, fixture.Pm);
        using var create = await CreateProjectUploadAsync(owner, fixture.Project, fixture.Task, "BEFORE");
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var unauthenticated = await CancelAsync(anonymous, id, ProjectUploads);
        unauthenticated.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertProblemAsync(await CancelAsync(owner, Guid.NewGuid(), ProjectUploads),
            HttpStatusCode.NotFound, "upload_session_not_found");
        await AssertProblemAsync(await CancelAsync(other, id, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        await AssertProblemAsync(await CancelAsync(pm, id, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");

        using var cancelled = await CancelAsync(owner, id, ProjectUploads);
        var body = await AssertCancelledAsync(cancelled, id);
        using var replay = await CancelAsync(owner, id, ProjectUploads);
        (await AssertCancelledAsync(replay, id)).Should().Be(body);
        using var second = await CreateProjectUploadAsync(owner, fixture.Project, fixture.Task, "MEASUREMENT");
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await using var db = sql.CreateDbContext();
        var task = await db.FieldInspectionTasks.SingleAsync(row => row.Id == fixture.Task);
        task.Reassign();
        await db.SaveChangesAsync();
        await AssertProblemAsync(await CancelAsync(owner, secondId, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        task.Transition(FieldInspectionTaskStatus.Accepted);
        await db.SaveChangesAsync();

        var member = await db.ProjectMembers.SingleAsync(row => row.ProjectId == fixture.Project && row.UserId == fixture.CrewId);
        member.Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync();
        await AssertProblemAsync(await CancelAsync(owner, id, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        member.Status = ProjectMemberStatus.Active;
        await db.SaveChangesAsync();

        var assignment = await db.FieldInspectionAssignments.SingleAsync(row => row.FieldInspectionTaskId == fixture.Task);
        assignment.End(DateTimeOffset.UtcNow, "Reassigned");
        await db.SaveChangesAsync();
        await AssertProblemAsync(await CancelAsync(owner, id, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        await AssertProblemAsync(await CancelAsync(owner, secondId, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        (await db.UploadSessions.AsNoTracking().SingleAsync(row => row.Id == secondId)).Status.Should().Be(UploadSessionStatus.Pending);
    }

    [Fact]
    public async Task CrewCancelFencesOldPartKeyAndRetriesMultipartCleanup()
    {
        var fixture = await SeedCrewTaskAsync();
        var remote = new RemotePhoto { FailNextAbort = true };
        await using var factory = Factory(remote, false);
        using var client = factory.CreateClient();
        await LoginAsync(client, fixture.Crew);
        using var create = await CreateProjectUploadAsync(client, fixture.Project, fixture.Task, "AFTER");
        var body = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();
        var file = body.GetProperty("fileId").GetGuid();
        var key = Guid.NewGuid().ToString();
        using var issued = await PartsAsync(client, id, key, route: ProjectUploads);
        issued.StatusCode.Should().Be(HttpStatusCode.OK);
        using var session = await client.GetAsync($"{ProjectUploads}/{id}");
        using var cancel = await CancelAsync(client, id, ProjectUploads);
        await AssertCancelledAsync(cancel, id);
        await AssertProblemAsync(await PartsAsync(client, id, key, route: ProjectUploads), HttpStatusCode.Conflict, "duplicate_request");
        var completeKey = Guid.NewGuid().ToString();
        var version = session.Headers.ETag!.ToString();
        using var complete = CompleteRequest(id, version, ProjectUploads, completeKey);
        await AssertProblemAsync(await client.SendAsync(complete), HttpStatusCode.Conflict, "duplicate_request");
        using var repeatComplete = CompleteRequest(id, version, ProjectUploads, completeKey);
        await AssertProblemAsync(await client.SendAsync(repeatComplete), HttpStatusCode.Conflict, "duplicate_request");
        remote.Signatures.Should().Be(1);

        await RecoverAsync(factory);
        remote.AbortAttempts.Should().Be(1);
        remote.Aborted.Should().BeFalse();
        await using (var db = sql.CreateDbContext())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET MultipartNextCheckAt={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={id}");
        await RecoverAsync(factory);
        remote.AbortAttempts.Should().Be(2);
        remote.Aborted.Should().BeTrue();
        remote.LastAbort.Should().Be(($"uploads/{file:N}", "fixture-id"));
    }

    [Fact]
    public async Task CrewCancelAndCompleteSerializeAndPreserveAcceptedEvidence()
    {
        var fixture = await SeedCrewTaskAsync();
        await using var factory = Factory(new RemotePhoto(), false);
        using var client = factory.CreateClient();
        await LoginAsync(client, fixture.Crew);
        using var create = await CreateProjectUploadAsync(client, fixture.Project, fixture.Task, "MEASUREMENT");
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var issued = await PartsAsync(client, id, Guid.NewGuid().ToString(), route: ProjectUploads);
        issued.StatusCode.Should().Be(HttpStatusCode.OK);
        using var session = await client.GetAsync($"{ProjectUploads}/{id}");
        using var complete = CompleteRequest(id, session.Headers.ETag!.ToString(), ProjectUploads);
        var cancelTask = CancelAsync(client, id, ProjectUploads);
        var completeTask = client.SendAsync(complete);
        await Task.WhenAll(cancelTask, completeTask);
        using var cancelled = await cancelTask;
        using var completed = await completeTask;
        await using var db = sql.CreateDbContext();
        var saved = await db.UploadSessions.AsNoTracking().SingleAsync(row => row.Id == id);
        if (cancelled.StatusCode == HttpStatusCode.OK)
        {
            await AssertCancelledAsync(cancelled, id);
            completed.StatusCode.Should().Be(HttpStatusCode.Conflict);
            saved.Status.Should().Be(UploadSessionStatus.Failed);
            saved.FailureCode.Should().Be("upload_cancelled");
        }
        else
        {
            await AssertProblemAsync(cancelled, HttpStatusCode.Conflict, "upload_not_cancellable");
            completed.StatusCode.Should().Be(HttpStatusCode.Accepted);
            saved.Status.Should().Be(UploadSessionStatus.Verifying);
        }

        using var acceptedCreate = await CreateProjectUploadAsync(client, fixture.Project, fixture.Task, "MEASUREMENT");
        var acceptedId = (await acceptedCreate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var acceptedParts = await PartsAsync(client, acceptedId, Guid.NewGuid().ToString(), route: ProjectUploads);
        acceptedParts.StatusCode.Should().Be(HttpStatusCode.OK);
        using var acceptedSession = await client.GetAsync($"{ProjectUploads}/{acceptedId}");
        var acceptedVersion = acceptedSession.Headers.ETag!.ToString();
        var acceptedKey = Guid.NewGuid().ToString();
        using var acceptedComplete = CompleteRequest(acceptedId, acceptedVersion, ProjectUploads, acceptedKey);
        using var accepted = await client.SendAsync(acceptedComplete);
        accepted.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await AssertProblemAsync(await CancelAsync(client, acceptedId, ProjectUploads), HttpStatusCode.Conflict, "upload_not_cancellable");
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IUploadService>();
            await service.ProcessOneVerificationAsync();
            if (saved.Status == UploadSessionStatus.Verifying) await service.ProcessOneVerificationAsync();
        }
        (await db.UploadSessions.AsNoTracking().SingleAsync(row => row.Id == acceptedId)).Status.Should().Be(UploadSessionStatus.Verified);
        await AssertProblemAsync(await CancelAsync(client, acceptedId, ProjectUploads), HttpStatusCode.Conflict, "upload_not_cancellable");
        using var replayComplete = CompleteRequest(acceptedId, acceptedVersion, ProjectUploads, acceptedKey);
        using var replayAccepted = await client.SendAsync(replayComplete);
        replayAccepted.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await replayAccepted.Content.ReadAsStringAsync()).Should().Be(await accepted.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CrewCannotCancelSessionFailedForAnotherReason()
    {
        var fixture = await SeedCrewTaskAsync();
        await using var factory = Factory(new RemotePhoto(), false);
        using var client = factory.CreateClient();
        await LoginAsync(client, fixture.Crew);
        using var create = await CreateProjectUploadAsync(client, fixture.Project, fixture.Task, "BEFORE");
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var issued = await PartsAsync(client, id, Guid.NewGuid().ToString(), route: ProjectUploads);
        issued.StatusCode.Should().Be(HttpStatusCode.OK);
        using var session = await client.GetAsync($"{ProjectUploads}/{id}");
        using var complete = CompleteRequest(id, session.Headers.ETag!.ToString(), ProjectUploads);
        using var completed = await client.SendAsync(complete);
        completed.StatusCode.Should().Be(HttpStatusCode.Accepted);

        await using var db = sql.CreateDbContext();
        var saved = await db.UploadSessions.SingleAsync(row => row.Id == id);
        saved.MarkFailed("checksum_mismatch");
        await db.SaveChangesAsync();
        await AssertProblemAsync(await CancelAsync(client, id, ProjectUploads), HttpStatusCode.Conflict, "upload_not_cancellable");
        (await db.UploadSessions.AsNoTracking().SingleAsync(row => row.Id == id)).FailureCode.Should().Be("checksum_mismatch");
    }

    [Theory]
    [InlineData("SURVEY_VIDEO", "video/mp4", "capture.mp4")]
    [InlineData("TELEMETRY", "application/x-subrip", "capture.srt")]
    public async Task OperatorCancelRechecksOwnerPurposeAndCurrentSurveyAuthority(string purpose, string mime, string file)
    {
        var fixture = await SeedSurveyTaskAsync();
        await using var factory = Factory(new RemotePhoto(), false);
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        await LoginAsync(owner, fixture.Owner);
        await LoginAsync(other, fixture.Other);
        using var create = await CreateProjectUploadAsync(owner, fixture.Project, fixture.Task, purpose, mime, file);
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var document = await CreateProjectUploadAsync(owner, fixture.Project, fixture.Task, "DOCUMENT", "application/pdf", "fixture.pdf");
        var documentId = (await document.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await AssertProblemAsync(await CancelAsync(other, id, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        await AssertProblemAsync(await CancelAsync(owner, documentId, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        using var cancelled = await CancelAsync(owner, id, ProjectUploads);
        var response = await AssertCancelledAsync(cancelled, id);
        using var repeated = await CancelAsync(owner, id, ProjectUploads);
        (await AssertCancelledAsync(repeated, id)).Should().Be(response);

        await using var db = sql.CreateDbContext();
        var membership = await db.ProjectMembers.SingleAsync(row => row.ProjectId == fixture.Project && row.UserId == fixture.OwnerId);
        membership.Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync();
        await AssertProblemAsync(await CancelAsync(owner, id, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        membership.Status = ProjectMemberStatus.Active;
        await db.SaveChangesAsync();

        using var newUpload = await CreateProjectUploadAsync(owner, fixture.Project, fixture.Task, purpose, mime, file);
        var newId = (await newUpload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var assignment = await db.SurveyAssignments.SingleAsync(row => row.SurveyRequestId == fixture.Task);
        var now = DateTimeOffset.UtcNow;
        assignment.EndForReassignment(now, "Replacement operator");
        await db.SaveChangesAsync();
        db.Add(SurveyAssignment.Create(Guid.NewGuid(), fixture.Task, fixture.OtherId, assignment.AssignedByUserId,
            now, now, null, null, null, null));
        await db.SaveChangesAsync();
        await AssertProblemAsync(await CancelAsync(owner, id, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        await AssertProblemAsync(await CancelAsync(owner, newId, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        await AssertProblemAsync(await CancelAsync(other, newId, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");

        using var replacementUpload = await CreateProjectUploadAsync(other, fixture.Project, fixture.Task, purpose, mime, file);
        var replacementId = (await replacementUpload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var task = await db.SurveyRequests.SingleAsync(row => row.Id == fixture.Task);
        task.MarkSubmitted();
        await db.SaveChangesAsync();
        await AssertProblemAsync(await CancelAsync(other, replacementId, ProjectUploads), HttpStatusCode.Forbidden, "access_forbidden");
        (await db.UploadSessions.AsNoTracking().SingleAsync(row => row.Id == replacementId)).Status.Should().Be(UploadSessionStatus.Pending);
    }

    [Theory]
    [InlineData("SURVEY_VIDEO", "video/mp4", "capture.mp4")]
    [InlineData("TELEMETRY", "application/x-subrip", "capture.srt")]
    public async Task OperatorCancelBlocksOldPartReceiptAndRetriesAbort(string purpose, string mime, string file)
    {
        var fixture = await SeedSurveyTaskAsync();
        var remote = new RemotePhoto { FailNextAbort = true };
        await using var factory = Factory(remote, false);
        using var client = factory.CreateClient();
        await LoginAsync(client, fixture.Owner);
        using var create = await CreateProjectUploadAsync(client, fixture.Project, fixture.Task, purpose, mime, file);
        var session = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = session.GetProperty("id").GetGuid();
        var fileId = session.GetProperty("fileId").GetGuid();
        var partKey = Guid.NewGuid().ToString();
        using var issued = await PartsAsync(client, id, partKey, route: ProjectUploads);
        issued.StatusCode.Should().Be(HttpStatusCode.OK);
        using var state = await client.GetAsync($"{ProjectUploads}/{id}");
        using var cancelled = await CancelAsync(client, id, ProjectUploads);
        await AssertCancelledAsync(cancelled, id);

        await AssertProblemAsync(await PartsAsync(client, id, partKey, route: ProjectUploads), HttpStatusCode.Conflict, "duplicate_request");
        var completeKey = Guid.NewGuid().ToString();
        var version = state.Headers.ETag!.ToString();
        using var complete = CompleteRequest(id, version, ProjectUploads, completeKey);
        await AssertProblemAsync(await client.SendAsync(complete), HttpStatusCode.Conflict, "duplicate_request");
        using var completeRetry = CompleteRequest(id, version, ProjectUploads, completeKey);
        await AssertProblemAsync(await client.SendAsync(completeRetry), HttpStatusCode.Conflict, "duplicate_request");
        remote.Signatures.Should().Be(1);

        await RecoverAsync(factory);
        remote.AbortAttempts.Should().Be(1);
        remote.Aborted.Should().BeFalse();
        await using (var db = sql.CreateDbContext())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET MultipartNextCheckAt={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={id}");
        await RecoverAsync(factory);
        remote.AbortAttempts.Should().Be(2);
        remote.Aborted.Should().BeTrue();
        remote.LastAbort.Should().Be(($"uploads/{fileId:N}", "fixture-id"));
    }

    [Theory]
    [InlineData("SURVEY_VIDEO", "video/mp4", "capture.mp4")]
    [InlineData("TELEMETRY", "application/x-subrip", "capture.srt")]
    public async Task OperatorCancelAndCompleteAllowExactlyOneTransition(string purpose, string mime, string file)
    {
        var fixture = await SeedSurveyTaskAsync();
        await using var factory = Factory(new RemotePhoto(), false);
        using var client = factory.CreateClient();
        await LoginAsync(client, fixture.Owner);
        using var create = await CreateProjectUploadAsync(client, fixture.Project, fixture.Task, purpose, mime, file);
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var parts = await PartsAsync(client, id, Guid.NewGuid().ToString(), route: ProjectUploads);
        parts.StatusCode.Should().Be(HttpStatusCode.OK);
        using var session = await client.GetAsync($"{ProjectUploads}/{id}");
        using var complete = CompleteRequest(id, session.Headers.ETag!.ToString(), ProjectUploads);
        var cancelTask = CancelAsync(client, id, ProjectUploads);
        var completeTask = client.SendAsync(complete);
        await Task.WhenAll(cancelTask, completeTask);
        using var cancelled = await cancelTask;
        using var completed = await completeTask;
        await using var db = sql.CreateDbContext();
        var saved = await db.UploadSessions.AsNoTracking().SingleAsync(row => row.Id == id);
        if (cancelled.StatusCode == HttpStatusCode.OK)
        {
            await AssertCancelledAsync(cancelled, id);
            completed.StatusCode.Should().Be(HttpStatusCode.Conflict);
            saved.Status.Should().Be(UploadSessionStatus.Failed);
            saved.FailureCode.Should().Be("upload_cancelled");
        }
        else
        {
            await AssertProblemAsync(cancelled, HttpStatusCode.Conflict, "upload_not_cancellable");
            completed.StatusCode.Should().Be(HttpStatusCode.Accepted);
            saved.Status.Should().Be(UploadSessionStatus.Verifying);
            saved.FailureCode.Should().BeNull();
        }
    }

    private async Task<(Guid Project, Guid Task, Guid OwnerId, string Owner, Guid OtherId, string Other)> SeedSurveyTaskAsync()
    {
        var owner = await sql.CreateUserAsync($"operator-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var other = await sql.CreateUserAsync($"other-operator-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var pm = await sql.CreateUserAsync($"pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Survey upload cancel", null, 32648, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"ROAD-{Guid.NewGuid():N}");
        var task = SurveyRequest.Create(Guid.NewGuid(), project.Id, road.Id, null, pm.Id,
            SurveyType.Periodic, SurveyRequestStatus.Accepted, now);
        var assignment = SurveyAssignment.Create(Guid.NewGuid(), task.Id, owner.Id, pm.Id, now, now, null, null, null, null);
        await using var db = sql.CreateDbContext();
        db.AddRange(project, road, task, assignment,
            ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, new(2000, 1, 1)));
        foreach (var userId in new[] { owner.Id, other.Id })
            db.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = userId,
                RoleCode = UserRoleCode.DroneOperator,
                Status = ProjectMemberStatus.Active,
                ValidFrom = new(2000, 1, 1)
            });
        await db.SaveChangesAsync();
        return (project.Id, task.Id, owner.Id, owner.UserName!, other.Id, other.UserName!);
    }

    private const string ProjectUploads = "/api/v1/uploads";
    private const string ReporterUploads = "/api/v1/reporter-evidence/uploads";

    private async Task<(Guid Project, Guid Task, Guid CrewId, string Crew, string Other, string Pm)> SeedCrewTaskAsync()
    {
        var crew = await sql.CreateUserAsync($"crew-{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var other = await sql.CreateUserAsync($"other-crew-{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await sql.CreateUserAsync($"pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var reporter = await sql.CreateUserAsync($"reporter-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var scope = await H3FieldInspectionWorkflowHttpTests.Seed(sql, pm.Id, crew.Id, other.Id, reporter.Id);
        var now = DateTimeOffset.UtcNow;
        var task = FieldInspectionTask.CreateOperational(Guid.NewGuid(), $"FIELD-{Guid.NewGuid():N}", scope.Project,
            scope.Defect, null, "REPORTER", scope.Route, scope.Set, null, null, FieldInspectionPurpose.PostRepair,
            4, "{}", null, now.AddDays(1), pm.Id);
        task.Transition(FieldInspectionTaskStatus.Accepted);
        task.Transition(FieldInspectionTaskStatus.InProgress);
        await using var db = sql.CreateDbContext();
        db.AddRange(task, FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, crew.Id, pm.Id,
            now, null, FieldInspectionAssignmentStatus.Active, null));
        await db.SaveChangesAsync();
        return (scope.Project, task.Id, crew.Id, crew.UserName!, other.UserName!, pm.UserName!);
    }

    private static async Task<HttpResponseMessage> CreateProjectUploadAsync(HttpClient client, Guid project, Guid task,
        string purpose, string mediaType = "image/jpeg", string fileName = "crew.jpg")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ProjectUploads)
        {
            Content = JsonContent.Create(new
            {
                projectId = project,
                targetId = task,
                purpose,
                fileName,
                mediaType,
                sizeBytes = 4,
                checksumSha256 = RemotePhoto.Hash
            })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return response;
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        using (response)
        {
            response.StatusCode.Should().Be(status);
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be(code);
        }
    }

    [Fact]
    public async Task LegacyTerminalViaRealWorker_ReturnsConflictAndPermitsNewCreate()
    {
        var user = await sql.CreateUserAsync($"legacy-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var remote = new RemotePhoto(); await using var factory = Factory(remote, false);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") }); await LoginAsync(client, user.UserName!);
        var response = await CreateAsync(client); var body = await response.Content.ReadFromJsonAsync<JsonElement>(); var upload = body.GetProperty("id").GetGuid();
        await using (var db = sql.CreateDbContext())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET FailureCode='multipart_initiating:legacy' WHERE Id={upload}");
        using (var scope = factory.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<IUploadRepository>().RecoverMultipartsAsync();
        (await PartsAsync(client, upload, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var restarted = await CreateAsync(client); restarted.StatusCode.Should().Be(HttpStatusCode.Created);
        var newUpload = (await restarted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        newUpload.Should().NotBe(upload);
        (await PartsAsync(client, newUpload, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private AuthenticationWebApplicationFactory Factory(RemotePhoto remote, bool loseAck)
        => new(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new PhotoAdapter(remote, loseAck));
            services.Configure<UploadSessionOptions>(o => o.RecoveryRetrySeconds = 1);
        });
    private static async Task LoginAsync(HttpClient client, string name)
    { var r = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(name), password = "Current1!" }); r.EnsureSuccessStatusCode(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()); }
    private static Task<HttpResponseMessage> CreateAsync(HttpClient client)
    { var r = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reporter-evidence/uploads") { Content = JsonContent.Create(new { fileName = "synthetic.jpg", mediaType = "image/jpeg", sizeBytes = 4, checksumSha256 = RemotePhoto.Hash }) }; r.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); return client.SendAsync(r); }
    private static Task<HttpResponseMessage> PartsAsync(HttpClient client, Guid upload, string key, int[]? parts = null, string route = ReporterUploads)
    { var r = new HttpRequestMessage(HttpMethod.Post, $"{route}/{upload}/part-urls") { Content = JsonContent.Create(new { partNumbers = parts ?? [1] }) }; r.Headers.Add("Idempotency-Key", key); return client.SendAsync(r); }

    private static async Task<HttpResponseMessage> CreateForOwnerAsync(HttpClient client, string userName)
    {
        await LoginAsync(client, userName);
        var response = await CreateAsync(client);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return response;
    }

    private static Task<HttpResponseMessage> CancelAsync(HttpClient client, Guid id, string route = ReporterUploads)
        => client.PostAsync($"{route}/{id}/cancel", null);

    private static async Task<string> AssertCancelledAsync(HttpResponseMessage response, Guid id)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().Be(id);
        body.GetProperty("status").GetString().Should().Be("FAILED");
        body.GetProperty("failureCode").GetString().Should().Be("upload_cancelled");
        return body.GetRawText();
    }

    private static HttpRequestMessage CompleteRequest(Guid id, string etag, string route = ReporterUploads, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{route}/{id}/complete")
        {
            Content = JsonContent.Create(new { checksumSha256 = RemotePhoto.Hash, parts = new[] { new { partNumber = 1, eTag = "fixture" } } })
        };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return request;
    }

    private static async Task RecoverAsync(AuthenticationWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IUploadRepository>().RecoverMultipartsAsync();
    }

    private sealed class RemotePhoto
    {
        public static byte[] Bytes => [255, 216, 255, 0];
        public static string Hash => Convert.ToHexString(SHA256.HashData(Bytes)).ToLowerInvariant();
        public int Initiations { get; set; }
        public int Signatures { get; set; }
        public string? ObjectKey { get; set; }
        public bool FailNextAbort { get; set; }
        public bool Aborted { get; set; }
        public int AbortAttempts { get; set; }
        public (string Key, string Id) LastAbort { get; set; }
    }
    private sealed class PhotoAdapter(RemotePhoto remote, bool loseAck) : IUploadObjectStorage, IMultipartRecoveryStorage
    {
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
        { remote.ObjectKey = objectKey; remote.Initiations++; if (loseAck) throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Synthetic lost remote acknowledgement"); return Task.FromResult("fixture-id"); }
        public Task<IReadOnlyList<string>> ListMultipartIdsAsync(string exactObjectKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(remote.ObjectKey == exactObjectKey && !remote.Aborted ? ["fixture-id"] : []);
        public Task<bool> HasPartsAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AbortMultipartAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default)
        {
            remote.AbortAttempts++;
            remote.LastAbort = (objectKey, uploadId);
            if (remote.FailNextAbort)
            {
                remote.FailNextAbort = false;
                throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Synthetic transient abort failure");
            }
            remote.Aborted = true;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            remote.Signatures++;
            return Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(n => new PresignedUploadPart(n, "https://storage.invalid/part", expiresAt)).ToArray());
        }
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => Task.FromResult(new UploadObjectVerification(4, RemotePhoto.Hash, "image/jpeg"));
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream(RemotePhoto.Bytes));
    }
}
