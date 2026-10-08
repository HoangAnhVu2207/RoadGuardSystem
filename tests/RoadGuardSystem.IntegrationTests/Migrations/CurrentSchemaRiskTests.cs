using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Migrations;

/// <summary>Current catalog checks preserved from the superseded per-migration lifecycle tests.</summary>
public sealed class CurrentSchemaRiskTests(IdentitySqlServerFixture fixture) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task BaselinePreservesEveryLifecycleCatalogAssertion()
    {
        await using var db = fixture.CreateDbContext();
        string[] expectedTables =
        [
            "AuditLogs", "IdempotencyRecords", "OutboxMessages", "ConsumerEffectReceipts",
            "Roles", "Users", "Sessions", "RefreshTokens", "PasswordResetLogs", "AccountStatusChangeLogs",
            "Files", "Projects", "ProjectMembers", "HandoverDocuments",
            "RoadSections", "RoadSectionVersions", "Warranties", "SurveyPlans", "SurveyPlanPostponements",
            "SurveyRequests", "Surveys", "SurveyAssignments", "Flights", "SurveyFiles",
            "SurveyDataVersions", "QualityChecks", "SupplementarySurveyRequests",
            "ProcessingBlocks", "ProcessingJobs", "ProcessingAttempts", "AIModelVersions",
            "AIDetections", "DefectVerificationLogs", "FieldInspectionTasks"
        ];
        var tables = await db.Database.SqlQueryRaw<string>("SELECT name AS [Value] FROM sys.tables").ToArrayAsync();
        foreach (var table in expectedTables)
            Assert.Contains(table, tables);

        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.indexes WHERE name='UX_Files_StorageUri'").SingleAsync());
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.check_constraints WHERE name='CK_Files_Checksum_Sha256Lowercase'").SingleAsync());
        foreach (var trigger in new[] { "TR_Files_Immutable", "TR_ProcessingBlocks_Immutable", "TR_ProcessingAttempts_AppendOnly" })
            Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE name={trigger} AND is_disabled=0").SingleAsync());
        Assert.Equal(3, await db.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS [Value] FROM sys.columns
            WHERE (object_id=OBJECT_ID('SurveyPlans') AND name='OutputRequirements')
               OR (object_id=OBJECT_ID('SurveyRequests') AND name IN ('DueAt','OutputRequirements'))
            """).SingleAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task FreshBaselineKeepsSharedDomainTablesEmpty()
    {
        await using var db = fixture.CreateDbContext();
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM Reports").SingleAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM IncidentCases").SingleAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM CandidateSourceHeads").SingleAsync());
    }
}
