using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Services.Exports;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

public sealed partial class WebPmCoordinationHttpTests
{
    [Fact]
    public async Task ActiveExactFtSupervisorAssignmentGrantsOnlyScopedExportAndRevocationBlocksReplay()
    {
        await using var minio = await OwnedWebMinio.StartAsync(createBucket: true);
        var seed = await Seed();
        Guid itemId;
        await using (var db = sql.CreateDbContext())
        {
            var package = await db.RepairPackages.Include(p => p.Items).Include(p => p.Obligations).SingleAsync(p => p.Id == seed.Package.Id);
            var obligation = package.Obligations.Single(o => o.Id != seed.Item.ObligationId);
            var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.FastTrack, seed.Pm.Id,
                UserRoleCode.ProjectManager, DateTimeOffset.UtcNow, new("FT scope", "ft-proof-v1"));
            package.AddItem(item); itemId = item.Id;
            db.Entry(obligation).Property(o => o.CurrentRepairItemId).CurrentValue = item.Id;
            db.Entry(package).Property<long>("MutationRevision").CurrentValue++;
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, allowExternalStorage: true,
            configureTestServices: services =>
            {
                minio.Configure(services);
                services.Configure<ExportOptions>(o => o.UnicodeFontPath = Environment.GetEnvironmentVariable("RG_TEST_UNICODE_FONT")
                    ?? throw new InvalidOperationException("Test font required."));
            });
        using var pm = await WebCookieTestClient.Login(factory, seed.Pm.Email!);
        using var sup = await WebCookieTestClient.Login(factory, seed.Supervisor.Email!);
        using var bearer = await Bearer(factory, seed.Supervisor);
        var exports = $"/api/v1/projects/{seed.Project.Id}/exports";
        var binding = $"/api/v1/projects/{seed.Project.Id}/repair-packages/{seed.Package.Id}/items/{itemId}/notification-supervisor";
        var key = Guid.NewGuid().ToString("N");
        async Task<HttpResponseMessage> Create(HttpClient client, Guid id, string? requestKey = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, exports)
            { Content = JsonContent.Create(new { kind = "DOSSIER", format = "PDF", repairItemIds = new[] { id } }) };
            request.Headers.Add("Idempotency-Key", requestKey ?? key); return await client.SendAsync(request);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await Create(sup, itemId)).StatusCode);
        using var assign = new HttpRequestMessage(HttpMethod.Put, binding) { Content = JsonContent.Create(new { supervisorId = seed.Supervisor.Id }) };
        assign.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N")); assign.Headers.Add("If-None-Match", "*");
        using var assigned = await pm.SendAsync(assign); await Body(assigned, HttpStatusCode.Created);
        var created = await Body(await Create(sup, itemId), HttpStatusCode.Accepted);
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal(id, (await Body(await Create(bearer, itemId), HttpStatusCode.Accepted)).GetProperty("id").GetGuid());
        Assert.Equal("idempotency_conflict", (await Body(await Create(sup, seed.Item.Id), HttpStatusCode.Conflict)).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await Create(sup, seed.Item.Id, Guid.NewGuid().ToString("N"))).StatusCode);
        sup.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.Forbidden, (await Create(sup, itemId)).StatusCode);
        await WebCookieTestClient.RefreshCsrf(sup);
        for (var n = 0; n < 10; n++)
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default);
            var status = (await Body(await sup.GetAsync(exports + $"/{id}"))).GetProperty("status").GetString();
            Assert.NotEqual("FAILED", status); if (status == "SUCCEEDED") break;
        }
        using var bytes = await sup.GetAsync(exports + $"/{id}/content");
        Assert.Equal(HttpStatusCode.OK, bytes.StatusCode);
        Assert.True((await bytes.Content.ReadAsByteArrayAsync()).AsSpan(0, 5).SequenceEqual("%PDF-"u8));
        using var current = await pm.GetAsync(binding); await Body(current);
        using var revoke = new HttpRequestMessage(HttpMethod.Delete, binding);
        revoke.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N")); revoke.Headers.TryAddWithoutValidation("If-Match", current.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.NoContent, (await pm.SendAsync(revoke)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Create(sup, itemId)).StatusCode);
        foreach (var suffix in new[] { "", "/manifest", "/content" })
            Assert.Equal(HttpStatusCode.Forbidden, (await sup.GetAsync(exports + $"/{id}" + suffix)).StatusCode);
        // Scope affects reads only: no membership is granted and the immutable snapshot is unchanged.
        await using var verify = sql.CreateDbContext();
        Assert.False(await verify.ProjectMembers.AnyAsync(m => m.ProjectId == seed.Project.Id && m.UserId == seed.Supervisor.Id));
        var snapshot = await verify.Set<RoadGuardSystem.BusinessObjects.Exports.ExportSnapshot>().SingleAsync(s => s.Id == created.GetProperty("snapshotId").GetGuid());
        Assert.Equal(created.GetProperty("snapshotHash").GetString(), snapshot.Hash);
    }
}
