using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.ApiTests.Files;

[Collection(AuthenticationApiFixture.Name)]
public sealed class WebPmVideoRangeHttpTests(AuthenticationSqlServerFixture sql)
{
    [MinioSmokeFact]
    public async Task RealMinioRangesPreserveBytesAndCurrentWebAndOperatorAuthority()
    {
        var stamp = Guid.NewGuid().ToString("N"); var now = DateTimeOffset.UtcNow;
        var sup = await sql.CreateUserAsync("range-sup-" + stamp, "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync("range-pm-" + stamp, "Current1!", UserRoleCode.ProjectManager);
        var op = await sql.CreateUserAsync("range-op-" + stamp, "Current1!", UserRoleCode.DroneOperator);
        var other = await sql.CreateUserAsync("range-other-" + stamp, "Current1!", UserRoleCode.DroneOperator);
        var project = Project.Create(Guid.NewGuid(), "RANGE-" + stamp, "Range test", null, 32648, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "RANGE");
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true,
            new LineString([new(500000, 1100000), new(500100, 1100000)]) { SRID = 32648 }, now, "test");
        var task = SurveyRequest.Create(Guid.NewGuid(), project.Id, road.Id, null, pm.Id, SurveyType.Original,
            SurveyRequestStatus.Submitted, now, roadSectionVersionId: route.Id);
        var assignment = SurveyAssignment.Create(Guid.NewGuid(), task.Id, op.Id, pm.Id, now, now, null, null, null, null);
        var survey = Survey.Create(Guid.NewGuid(), task.Id, project.Id, route.Id, SurveyType.Original, SurveyStatus.Completed, false, null, null);
        var bytes = Enumerable.Range(0, 1024 * 1024 + 17).Select(index => (byte)(index % 251)).ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(); var id = Guid.NewGuid(); var key = $"uploads/{id:N}";
        var file = StoredFile.Create(id, key, "video.mp4", "video/mp4", bytes.Length, hash, op.Id, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), id, op.Id, key, "SURVEY_VIDEO", "video/mp4", bytes.Length, hash, 8388608, now.AddHours(1));
        upload.StartUploading("fixture", now);
        var manifest = JsonSerializer.Serialize(new[] { new { fileId = id, purpose = "SURVEY_VIDEO" } });
        var data = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed,
            SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, manifest);
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(project, road, route, task, assignment, survey, data, file, upload,
                FileScope.Create(Guid.NewGuid(), id, project.Id, task.Id, op.Id, "SURVEY_VIDEO", now),
                ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, DateOnly.FromDateTime(now.UtcDateTime)),
                new ProjectMember
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    UserId = op.Id,
                    RoleCode = UserRoleCode.DroneOperator,
                    ValidFrom = DateOnly.FromDateTime(now.UtcDateTime),
                    Status = ProjectMemberStatus.Active
                });
            await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, allowExternalStorage: true);
        var options = factory.Services.GetRequiredService<IOptions<MinioStorageOptions>>().Value;
        using var s3 = new AmazonS3Client(options.AccessKey, options.SecretKey, new AmazonS3Config
        { ServiceURL = options.Endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.BucketName,
            Key = key,
            InputStream = new MemoryStream(bytes),
            ContentType = "video/mp4"
        });
        try
        {
            using (var storageScope = factory.Services.CreateScope())
            {
                var storage = Assert.IsAssignableFrom<IRangedUploadObjectStorage>(storageScope.ServiceProvider.GetRequiredService<IUploadObjectStorage>());
                await using (var cancelledRead = await storage.OpenRangeAsync(key, 10, 19))
                {
                    using var cancelled = new CancellationTokenSource();
                    await cancelled.CancelAsync();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledRead.CopyToAsync(Stream.Null, cancelled.Token));
                }
                await using var reopened = await storage.OpenRangeAsync(key, 10, 19);
                using var selected = new MemoryStream();
                await reopened.CopyToAsync(selected);
                Assert.Equal(bytes[10..20], selected.ToArray());
            }
            using var pmWeb = await WebCookieTestClient.Login(factory, pm.Email!);
            using var supWeb = await WebCookieTestClient.Login(factory, sup.Email!);
            using var operatorBearer = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
            using var login = await operatorBearer.PostAsJsonAsync("/api/v1/auth/login", new { email = op.Email, password = "Current1!" });
            operatorBearer.DefaultRequestHeaders.Authorization = new("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
            var path = $"/api/v1/files/{id}/content";
            using var full = await pmWeb.GetAsync(path); Assert.Equal(HttpStatusCode.OK, full.StatusCode);
            Assert.Equal(bytes, await full.Content.ReadAsByteArrayAsync()); Assert.Equal(bytes.Length, full.Content.Headers.ContentLength);
            Assert.Equal("video/mp4", full.Content.Headers.ContentType!.MediaType); Assert.Contains("bytes", full.Headers.AcceptRanges);
            var etag = full.Headers.ETag!.ToString();
            foreach (var client in new[] { pmWeb, supWeb, operatorBearer })
                foreach (var range in new[] { ("bytes=0-7", 0, 7), ("bytes=1048576-", 1048576, bytes.Length - 1), ("bytes=-17", bytes.Length - 17, bytes.Length - 1), ("bytes=9-99999999", 9, bytes.Length - 1) })
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, path); request.Headers.TryAddWithoutValidation("Range", range.Item1);
                    request.Headers.TryAddWithoutValidation("If-Range", etag);
                    using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
                    Assert.Equal(bytes[range.Item2..(range.Item3 + 1)], await response.Content.ReadAsByteArrayAsync());
                    Assert.Equal($"bytes {range.Item2}-{range.Item3}/{bytes.Length}", response.Content.Headers.ContentRange!.ToString());
                    Assert.Equal(range.Item3 - range.Item2 + 1, response.Content.Headers.ContentLength);
                }
            foreach (var range in new[] { "bytes=999999999-", "bytes=5-4", "bytes=-0", "bytes=a-b", "items=0-1", "bytes=0-1,3-4" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path); request.Headers.TryAddWithoutValidation("Range", range);
                using var response = await pmWeb.SendAsync(request); Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, response.StatusCode);
                Assert.Equal($"bytes */{bytes.Length}", response.Content.Headers.ContentRange!.ToString());
            }
            foreach (var validator in new[] { "\"stale\"", "W/" + etag, "Wed, 18 Nov 2026 00:00:00 GMT" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path); request.Headers.TryAddWithoutValidation("Range", "bytes=0-1");
                request.Headers.TryAddWithoutValidation("If-Range", validator);
                using var response = await pmWeb.SendAsync(request); Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(bytes.Length, response.Content.Headers.ContentLength);
            }
            using var otherBearer = factory.CreateClient(new() { HandleCookies = false });
            using var otherLogin = await otherBearer.PostAsJsonAsync("/api/v1/auth/login", new { email = other.Email, password = "Current1!" });
            otherBearer.DefaultRequestHeaders.Authorization = new("Bearer", (await otherLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.Forbidden, (await otherBearer.GetAsync(path)).StatusCode);
            await using var changed = sql.CreateDbContext();
            await changed.SurveyAssignments.Where(row => row.Id == assignment.Id).ExecuteUpdateAsync(set => set.SetProperty(row => row.EndedAt, DateTimeOffset.UtcNow));
            Assert.Equal(HttpStatusCode.Forbidden, (await operatorBearer.GetAsync(path)).StatusCode);
            await changed.ProjectMembers.Where(row => row.ProjectId == project.Id && row.UserId == pm.Id).ExecuteUpdateAsync(set => set.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
            Assert.Equal(HttpStatusCode.Forbidden, (await pmWeb.GetAsync(path)).StatusCode);
        }
        finally { await s3.DeleteObjectAsync(options.BucketName, key); }
    }
}
