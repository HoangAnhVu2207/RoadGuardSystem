using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoadGuardSystem.API.Workers;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.ApiTests.Startup;

public sealed class UploadWorkerCompositionTests
{
    [Theory]
    [InlineData("Production", "127.0.0.1:1", true)]
    [InlineData("Development", "127.0.0.1:1", true)]
    [InlineData("Production", "", false)]
    [InlineData("Testing", "", false)]
    public async Task ConfiguredStorageRegistersVerifierAcrossEnvironments(string environment, string endpoint, bool expected)
    {
        await using var factory = new WorkerCompositionFactory(environment, endpoint);
        using var client = factory.CreateClient();
        Assert.Equal(expected ? 1 : 0, factory.VerifierRegistrations);
    }

    [Fact]
    public void IntegratedModelMatchesSnapshotAndOrdersAdditiveAiMigrationsBeforeHuyTriggers()
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=DisconnectedModelInspection;Integrated Security=true", sql => sql.UseNetTopologySuite())
            .Options;
        using var db = new RoadGuardDbContext(options);
        Assert.False(db.Database.HasPendingModelChanges());
        var migrations = db.Database.GetMigrations().ToArray();
        var worker = Array.IndexOf(migrations, "20261119161424_AddAiWorkerTransportLedger");
        var telemetry = Array.IndexOf(migrations, "20261119164737_AddAiVideoTelemetryRevisions");
        var outcomes = Array.IndexOf(migrations, "20261119170649_AddAiVideoAttemptOutcomes");
        var field = Array.IndexOf(migrations, "20261123093000_FieldTaskMeasurementDimensions");
        var safety = Array.IndexOf(migrations, "20261123093100_InitialTemporarySafetyResponsibility");
        Assert.True(worker >= 0 && worker < telemetry && telemetry < outcomes && outcomes < field && field < safety);
        var sql = db.GetService<IMigrator>().GenerateScript(migrations[worker - 1], migrations[safety]);
        Assert.Contains("CREATE TABLE [AiVideoWorkItems]", sql);
        Assert.Contains("CREATE TABLE [AiVideoTelemetryRevisions]", sql);
        Assert.Contains("CREATE TABLE [AiVideoAttemptOutcomes]", sql);
        Assert.Contains("CREATE OR ALTER TRIGGER [TR_FieldInspectionTasks_H3Scope]", sql);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class WorkerCompositionFactory(string environment, string endpoint) : CustomWebApplicationFactory(environment)
    {
        public int VerifierRegistrations { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Observe the real Program registrations before the platform fixture removes every
            // hosted worker. No worker, SQL connection or storage call runs in this test.
            builder.ConfigureServices((_, services) => VerifierRegistrations = services.Count(x =>
                x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(UploadVerificationWorker)));
            base.ConfigureWebHost(builder);
            builder.UseSetting("MinioStorage:Endpoint", endpoint);
            builder.UseSetting("Anh02:WorkersEnabled", "false");
            builder.UseSetting("AiRealWorker:Enabled", "false");
        }
    }
}
