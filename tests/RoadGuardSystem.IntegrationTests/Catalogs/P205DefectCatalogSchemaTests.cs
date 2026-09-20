using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Catalogs;

[Trait("TaskId", "P2-05")]
public sealed class P205DefectCatalogSchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P205DefectCatalogSchemaTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-05: Defect type catalog round-trips through SQL Server")]
    public async Task DefectType_ValidRecord_RoundTrips()
    {
        var code = $"P205-DT-{Guid.NewGuid():N}";
        var defectType = DefectType.Create(code, "Longitudinal cracking", "Cracks along the travel direction.");

        await using (var context = _fixture.CreateDbContext())
        {
            context.DefectTypes.Add(defectType);
            await context.SaveChangesAsync();
        }

        await using var readContext = _fixture.CreateDbContext();
        var persisted = await readContext.DefectTypes
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Code == code);

        persisted.Name.Should().Be("Longitudinal cracking");
        persisted.Description.Should().Be("Cracks along the travel direction.");
        persisted.IsActive.Should().BeTrue();
    }

    [Fact(DisplayName = "P2-05: duplicate defect type catalog keys are rejected")]
    public async Task DefectType_DuplicateCode_IsRejectedBySqlServer()
    {
        var code = $"P205-DT-{Guid.NewGuid():N}";
        await using (var seedContext = _fixture.CreateDbContext())
        {
            seedContext.DefectTypes.Add(DefectType.Create(code, "First label"));
            await seedContext.SaveChangesAsync();
        }

        await using var context = _fixture.CreateDbContext();
        context.DefectTypes.Add(DefectType.Create(code, "Second label"));

        var persist = () => context.SaveChangesAsync();

        await persist.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-05: duplicate cause category catalog keys are rejected")]
    public async Task CauseCategory_DuplicateCode_IsRejectedBySqlServer()
    {
        var code = $"P205-CC-{Guid.NewGuid():N}";
        await using (var seedContext = _fixture.CreateDbContext())
        {
            seedContext.CauseCategories.Add(CauseCategory.Create(code, "First cause"));
            await seedContext.SaveChangesAsync();
        }

        await using var context = _fixture.CreateDbContext();
        context.CauseCategories.Add(CauseCategory.Create(code, "Second cause"));

        var persist = () => context.SaveChangesAsync();

        await persist.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-05: severity rule scope and version round-trip through SQL Server")]
    public async Task SeverityRuleVersion_ValidRecord_RoundTrips()
    {
        var rule = SeverityRuleVersion.Create(
            Guid.NewGuid(),
            "P205-STANDARD",
            "P205-ROAD",
            1,
            "{\"threshold\":{\"minor\":10}}",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));

        await using var context = _fixture.CreateDbContext();
        context.SeverityRuleVersions.Add(rule);
        await context.SaveChangesAsync();

        var persisted = await context.SeverityRuleVersions
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == rule.Id);

        persisted.StandardCode.Should().Be("P205-STANDARD");
        persisted.RoadTypeCode.Should().Be("P205-ROAD");
        persisted.VersionNo.Should().Be(1);
        persisted.RuleDefinition.Should().Be("{\"threshold\":{\"minor\":10}}");
        persisted.EffectiveFrom.Should().Be(new DateOnly(2026, 1, 1));
        persisted.EffectiveTo.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact(DisplayName = "P2-05: duplicate severity rule scope versions are rejected")]
    public async Task SeverityRuleVersion_DuplicateScopeVersion_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var standardCode = $"P205-STANDARD-{Guid.NewGuid():N}";
        context.SeverityRuleVersions.AddRange(
            CreateRuleVersion(1, standardCode),
            CreateRuleVersion(1, standardCode));

        var persist = () => context.SaveChangesAsync();

        await persist.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-05: non-object rule JSON is rejected by SQL Server")]
    public async Task SeverityRuleVersion_NonObjectJson_IsRejectedBySqlServer()
    {
        var id = Guid.NewGuid();
        await using var context = _fixture.CreateDbContext();

        var insert = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SeverityRuleVersions]
                ([Id], [StandardCode], [RoadTypeCode], [VersionNo], [RuleDefinition], [EffectiveFrom], [EffectiveTo])
            VALUES ({id}, {"P205-INVALID"}, {"P205-ROAD"}, {1}, {"[1,2,3]"}, {new DateOnly(2026, 1, 1)}, {null})
            """);

        await insert.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-05: malformed rule JSON is rejected by SQL Server")]
    public async Task SeverityRuleVersion_MalformedJson_IsRejectedBySqlServer()
    {
        var id = Guid.NewGuid();
        await using var context = _fixture.CreateDbContext();

        var insert = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SeverityRuleVersions]
                ([Id], [StandardCode], [RoadTypeCode], [VersionNo], [RuleDefinition], [EffectiveFrom], [EffectiveTo])
            VALUES ({id}, {"P205-MALFORMED"}, {"P205-ROAD"}, {1}, {"{invalid"}, {new DateOnly(2026, 1, 1)}, {null})
            """);

        await insert.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-05: severity rule version fields are immutable after insert")]
    public async Task SeverityRuleVersion_TrackedMutation_IsRejected()
    {
        var rule = CreateRuleVersion(1);
        await using var context = _fixture.CreateDbContext();
        context.SeverityRuleVersions.Add(rule);
        await context.SaveChangesAsync();

        context.Entry(rule).Property(candidate => candidate.RuleDefinition).CurrentValue = "{\"threshold\":{\"minor\":20}}";
        var update = () => context.SaveChangesAsync();

        await update.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact(DisplayName = "P2-05: referenced catalog rows cannot be deleted")]
    public async Task Defect_ReferencedCatalogRows_CannotBeDeleted()
    {
        var defectTypeCode = $"P205-REFERENCED-DT-{Guid.NewGuid():N}";
        var causeCategoryCode = $"P205-REFERENCED-CC-{Guid.NewGuid():N}";
        await using (var seedContext = _fixture.CreateDbContext())
        {
            seedContext.DefectTypes.Add(DefectType.Create(defectTypeCode, "Referenced defect type"));
            seedContext.CauseCategories.Add(CauseCategory.Create(causeCategoryCode, "Referenced cause"));
            seedContext.Defects.Add(Defect.Create(Guid.NewGuid(), defectTypeCode, causeCategoryCode));
            await seedContext.SaveChangesAsync();
        }

        await using var context = _fixture.CreateDbContext();
        var deleteDefectType = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [DefectTypes] WHERE [Code] = {defectTypeCode}");
        var deleteCauseCategory = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [CauseCategories] WHERE [Code] = {causeCategoryCode}");

        await deleteDefectType.Should().ThrowAsync<SqlException>();
        await deleteCauseCategory.Should().ThrowAsync<SqlException>();
    }

    private static SeverityRuleVersion CreateRuleVersion(int version, string? standardCode = null)
        => SeverityRuleVersion.Create(
            Guid.NewGuid(),
            standardCode ?? "P205-STANDARD-" + Guid.NewGuid().ToString("N"),
            "P205-ROAD",
            version,
            "{\"threshold\":{\"minor\":10}}",
            new DateOnly(2026, 1, 1));
}
