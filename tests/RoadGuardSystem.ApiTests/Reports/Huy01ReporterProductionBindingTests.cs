using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

public sealed partial class Huy01ReporterReportsApiTests
{
    [Fact]
    public void Composition_root_registers_one_scoped_real_reporter_service_and_repository()
    {
        Microsoft.Extensions.DependencyInjection.ServiceDescriptor[] registrations = [];
        using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services => registrations = services.ToArray());
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Reports.IReporterReportService>();
        var repository = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.Reports.IReporterReportRepository>();
        Assert.IsType<RoadGuardSystem.Services.Implementations.Reports.ReporterReportService>(service);
        Assert.IsType<RoadGuardSystem.Repositories.Implementations.Reports.ReporterReportRepository>(repository);
        Assert.Same(repository, scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.Reports.IReporterReportRepository>());
        Assert.Same(service, scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Reports.IReporterReportService>());
        foreach (var type in new[] { typeof(RoadGuardSystem.Services.Reports.IReporterReportService), typeof(RoadGuardSystem.Repositories.Reports.IReporterReportRepository) })
            Assert.Equal(ServiceLifetime.Scoped, Assert.Single(registrations.Where(d => d.ServiceType == type)).Lifetime);
    }

    [Theory]
    [InlineData("inactive", false)] [InlineData("inactive", true)]
    [InlineData("password", false)] [InlineData("password", true)]
    [InlineData("role", false)] [InlineData("role", true)]
    [InlineData("pending", false)] [InlineData("pending", true)]
    [InlineData("failed", false)] [InlineData("failed", true)]
    [InlineData("version", false)] [InlineData("version", true)]
    public async Task Production_binding_denies_protected_outcome_after_preflight(string mutation, bool conflict)
    {
        var reporter = await sql.CreateUserAsync($"reporter-binding-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        Guid fileId = Guid.Empty;
        var barrier = new Anh02ReceiptRevocationInterceptor(async token =>
        {
            await using var db = sql.CreateDbContext();
            switch (mutation)
            {
                case "inactive": await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status=2 WHERE Id={reporter.Id}", token); break;
                case "password": await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET MustChangePassword=1 WHERE Id={reporter.Id}", token); break;
                case "role": await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET RoleCode={UserRoleCode.ProjectManager.ToDbCode()} WHERE Id={reporter.Id}", token); break;
                case "pending": await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET Status=1 WHERE FileId={fileId}", token); break;
                case "failed": await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET Status=5 WHERE FileId={fileId}", token); break;
                default: await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET StorageUploadId={Guid.NewGuid().ToString()} WHERE FileId={fileId}", token); break;
            }
        });
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.RemoveAll<RoadGuardDbContext>();
            services.AddScoped(sp => new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>(sp.GetRequiredService<DbContextOptions<RoadGuardDbContext>>()).AddInterceptors(barrier).Options));
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var file = await UploadVerifiedAsync(client, factory); fileId = file.FileId;
        object Payload(bool changed) => new { description = changed ? "Changed binding payload" : "Binding report", evidence = new[] { new { fileId, fileVersion = file.Version, locationSource = "UNKNOWN" } } };
        var key = Guid.NewGuid().ToString();
        var first = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", Payload(false), key);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var report = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var before = await CountIntakeRowsAsync(sql, reporter.Id, report, key);
        barrier.Armed = true;
        var denied = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", Payload(conflict), key);
        var status = mutation switch { "pending" or "failed" => HttpStatusCode.Conflict, "version" => HttpStatusCode.PreconditionFailed, _ => HttpStatusCode.Forbidden };
        Assert.Equal(status, denied.StatusCode);
        var code = mutation switch { "pending" or "failed" => "source_not_ready", "version" => "concurrency_conflict", _ => "access_forbidden" };
        Assert.Equal(code, (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Null(denied.Headers.ETag); Assert.Null(denied.Headers.Location); Assert.Equal(1, barrier.Calls);
        Assert.Equal(before, await CountIntakeRowsAsync(sql, reporter.Id, report, key));
    }
}
