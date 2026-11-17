using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using RoadGuardSystem.BusinessObjects.Files;

namespace RoadGuardSystem.ApiTests.Infrastructure;

internal static class VerifiedProjectHandoverFixture
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    // SQL fixture preparation is explicit. HTTP project creation still uses the production
    // finalize path; storage and multipart are exercised separately by staging tests.
    internal static async Task<HttpResponseMessage> CreateProjectAsync(AuthenticationSqlServerFixture sql,
        HttpClient client, object body)
    {
        var token = client.DefaultRequestHeaders.Authorization?.Parameter;
        if (token is null) throw new InvalidOperationException("Legacy project fixture requires a Supervisor Bearer.");
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;
        var actor = Guid.Parse(claims.Single(claim => claim.Type == "sub").Value);
        var identity = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(actor.ToString() + JsonSerializer.Serialize(body, Json))).AsSpan(0, 16));
        var staged = await StageAsync(sql, actor, requestedFileId: identity);
        var payload = JsonSerializer.SerializeToNode(body, Json)!.AsObject();
        if (payload.ContainsKey("code"))
            payload["handoverFileIds"] = new JsonArray(JsonValue.Create(staged.FileId));
        else if (payload["handover"] is JsonObject handover)
            handover["fileId"] = JsonValue.Create(staged.FileId);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects") { Content = JsonContent.Create(payload) };
        request.Headers.TryAddWithoutValidation("If-Match", staged.ETag);
        return await client.SendAsync(request);
    }

    internal static async Task<(Guid FileId, string ETag)> StageAsync(AuthenticationSqlServerFixture sql, Guid actor, DateTimeOffset? stagedAt = null, Guid? requestedFileId = null)
    {
        var now = stagedAt ?? DateTimeOffset.UtcNow;
        var fileId = requestedFileId ?? Guid.NewGuid();
        await using var db = sql.CreateDbContext();
        var existing = await db.UploadSessions.AsNoTracking().SingleOrDefaultAsync(row => row.FileId == fileId);
        if (existing is not null) return (fileId, '"' + Convert.ToBase64String(existing.RowVersion) + '"');
        var key = $"uploads/{fileId:N}";
        var checksum = new string('b', 64);
        var file = StoredFile.Create(fileId, key, "fixture.pdf", "application/pdf", 100, checksum, actor, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), fileId, actor, key, "DOCUMENT", "application/pdf",
            100, checksum, 8 * 1024 * 1024, now.AddHours(1));
        upload.StartUploading("fixture", now);
        db.AddRange(file, upload, FileScope.CreateHandoverStaging(Guid.NewGuid(), fileId, actor, now));
        await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await db.SaveChangesAsync();
        upload.MarkVerified();
        await db.SaveChangesAsync();
        return (fileId, '"' + Convert.ToBase64String(upload.RowVersion) + '"');
    }
}
