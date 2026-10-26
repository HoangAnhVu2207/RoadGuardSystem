using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Seeding;
using RoadGuardSystem.Repositories.Storage;

// Disposable fault acceptance helper only; no public endpoint, inherited app/local config or arbitrary SQL.
var run = Guid.ParseExact(Required("RGM_RUN"), "N").ToString("N");
if (args[0] == "cleanup-owned-run")
{
    var path = Path.GetFullPath(args[1]);
    var allowed = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "multipart-recovery"));
    if (Path.GetDirectoryName(path) != allowed || Path.GetFileName(path) != $"live-{run}.json")
        throw new InvalidOperationException("Only this task's recorded run evidence is accepted.");
    using var proof = JsonDocument.Parse(await File.ReadAllTextAsync(path));
    var expectedBucket = "roadguard-multipart-" + run;
    if (proof.RootElement.GetProperty("run").GetString() != run || proof.RootElement.GetProperty("bucket").GetString() != expectedBucket
        || proof.RootElement.GetProperty("targetVerified").GetProperty("bucket").GetString() != expectedBucket
        || proof.RootElement.GetProperty("result").GetString() != "FAIL")
        throw new InvalidOperationException("Failed isolated run ownership proof mismatch.");
    if (Required("RGM_STORAGE") != "http://127.0.0.1:19000") throw new InvalidOperationException("Exact endpoint mismatch.");
    using var client = new AmazonS3Client(new BasicAWSCredentials(Required("RGM_ACCESS"), Required("RGM_SECRET")),
        new AmazonS3Config { ServiceURL = Required("RGM_STORAGE"), ForcePathStyle = true });
    var uploads = new List<MultipartUpload>(); string? keyMarker = null, idMarker = null;
    do
    {
        var page = await client.ListMultipartUploadsAsync(new ListMultipartUploadsRequest { BucketName = expectedBucket, Prefix = "uploads/", KeyMarker = keyMarker, UploadIdMarker = idMarker });
        foreach (var upload in page.MultipartUploads ?? [])
            if (upload.Key.StartsWith("uploads/", StringComparison.Ordinal) && Guid.TryParseExact(upload.Key[8..], "N", out _)) uploads.Add(upload);
            else throw new InvalidOperationException("Unexpected key; cleanup fails closed.");
        if (page.IsTruncated != true) break;
        if (page.NextKeyMarker == keyMarker && page.NextUploadIdMarker == idMarker) throw new InvalidOperationException("Inventory pagination stalled.");
        keyMarker = page.NextKeyMarker; idMarker = page.NextUploadIdMarker;
    } while (true);
    var execute = args.Length == 3 && args[2] == "--execute";
    if (execute)
        foreach (var upload in uploads)
            try { await client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest { BucketName = expectedBucket, Key = upload.Key, UploadId = upload.UploadId }); }
            catch (AmazonS3Exception e) when (e.ErrorCode == "NoSuchUpload") { }
    Console.WriteLine(JsonSerializer.Serialize(new { run, bucket = expectedBucket, dryRun = !execute, multipartCount = uploads.Count, objectsDeleted = 0 }));
    return;
}
var connection = new SqlConnectionStringBuilder(Required("RGM_SQL"));
if (connection.InitialCatalog != "RoadGuard_Multipart_" + run || !connection.DataSource.StartsWith("127.0.0.1,", StringComparison.Ordinal))
    throw new InvalidOperationException("Exact isolated SQL target mismatch.");
var master = new SqlConnectionStringBuilder(connection.ConnectionString) { InitialCatalog = "master" };
await using (var sql = new SqlConnection(master.ConnectionString))
{
    await sql.OpenAsync(); await using var probe = sql.CreateCommand(); probe.CommandText = "SELECT CAST(@@SERVERNAME AS nvarchar(128))";
    if (!string.Equals((string)(await probe.ExecuteScalarAsync())!, Required("RGM_SERVER"), StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("SQL instance identity mismatch.");
    if (args[0] == "init")
    { await using var create = sql.CreateCommand(); create.CommandText = $"IF DB_ID('{connection.InitialCatalog}') IS NULL CREATE DATABASE [{connection.InitialCatalog}]"; await create.ExecuteNonQueryAsync(); }
}
var options = new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(connection.ConnectionString, x => x.UseNetTopologySuite()).Options;
await using var db = new RoadGuardDbContext(options);
var storageOptions = new MinioStorageOptions { Endpoint = Required("RGM_STORAGE"), BucketName = "roadguard-multipart-" + run,
    AccessKey = Required("RGM_ACCESS"), SecretKey = Required("RGM_SECRET") };
if (storageOptions.Endpoint != "http://127.0.0.1:19000") throw new InvalidOperationException("Exact live test endpoint mismatch.");
var storage = new MinioUploadObjectStorage(Options.Create(storageOptions));
if (args[0] == "init")
{
    await db.Database.MigrateAsync(); await new IdentityRoleSeedStep().SeedAsync(db, default);
    foreach (var name in new[] { "owner", "other" })
    {
        var email = $"{name}-{run}@multipart.example.test";
        if (await db.Users.AnyAsync(u => u.Email == email)) continue;
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, NormalizedUserName = email.ToUpperInvariant(), Email = email,
            NormalizedEmail = email.ToUpperInvariant(), EmailConfirmed = true, DisplayName = "Synthetic multipart acceptance " + name,
            RoleCode = UserRoleCode.Reporter, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow };
        user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, Required("RGM_PASSWORD")); db.Users.Add(user);
    }
    await db.SaveChangesAsync();
    using var s3 = new AmazonS3Client(new BasicAWSCredentials(storageOptions.AccessKey, storageOptions.SecretKey), new AmazonS3Config { ServiceURL = storageOptions.Endpoint, ForcePathStyle = true });
    await s3.PutBucketAsync(new PutBucketRequest { BucketName = storageOptions.BucketName });
    Console.WriteLine(JsonSerializer.Serialize(new { database = connection.InitialCatalog, bucket = storageOptions.BucketName, pendingModelChanges = db.Database.HasPendingModelChanges() }));
}
else
{
    var id = Guid.Parse(args[1]); var session = await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == id);
    if (session.ObjectKey != $"uploads/{session.FileId:N}" || !await db.Files.AnyAsync(f => f.Id == session.FileId && f.StorageUri == session.ObjectKey))
        throw new InvalidOperationException("Object ownership proof failed.");
    if (args[0] == "duplicate") await storage.InitiateAsync(session.ObjectKey, session.MediaType);
    if (args[0] == "revoke") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status={(byte)UserStatus.Suspended} WHERE Id={session.OwnerUserId}");
    if (args[0] == "status" || args[0] == "duplicate")
    {
        var ids = await storage.ListMultipartIdsAsync(session.ObjectKey);
        Console.WriteLine(JsonSerializer.Serialize(new { session.Id, session.FileId, status = session.Status.ToString(), session.MultipartPhase,
            session.StorageUploadId, candidates = ids.Count,
            partReceipts = await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == session.OwnerUserId && r.Operation == "UploadPartUrlsIssued"),
            files = await db.Files.CountAsync(f => f.UploadedByUserId == session.OwnerUserId), audit = await db.AuditLogs.CountAsync(a => a.ActorUserId == session.OwnerUserId) }));
    }
}
static string Required(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException("Missing " + name);
