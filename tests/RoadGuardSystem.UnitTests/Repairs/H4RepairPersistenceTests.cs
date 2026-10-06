using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4RepairPersistenceTests
{
    private static RoadGuardDbContext Context() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(sql => sql.UseNetTopologySuite()).Options);

    [Theory]
    [InlineData(typeof(RepairPackage), "RepairPackages")]
    [InlineData(typeof(RepairObligation), "RepairObligations")]
    [InlineData(typeof(RepairItem), "RepairItems")]
    [InlineData(typeof(RepairAttempt), "RepairAttempts")]
    [InlineData(typeof(RepairDecision), "RepairDecisions")]
    [InlineData(typeof(RepairReviewRequest), "RepairReviewRequests")]
    public void NativeRepairHeadsAndHistoriesHaveDedicatedTypedPersistence(Type type, string table)
    {
        using var db = Context(); var entity = db.Model.FindEntityType(type);
        Assert.NotNull(entity); Assert.Equal(table, entity.GetTableName());
        Assert.NotNull(entity.FindPrimaryKey());
        Assert.All(entity.GetForeignKeys(), foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    [Theory]
    [InlineData(typeof(RepairPackage))]
    [InlineData(typeof(RepairObligation))]
    [InlineData(typeof(RepairItem))]
    public void MutableRepairHeadsUseDatabaseConcurrencyTokens(Type type)
    {
        using var db = Context(); var entity = db.Model.FindEntityType(type); Assert.NotNull(entity);
        var version = entity.FindProperty("RowVersion"); Assert.NotNull(version); Assert.True(version.IsConcurrencyToken);
    }
    [Theory]
    [InlineData(typeof(RepairItem), "EffectiveDecisionId")]
    [InlineData(typeof(RepairObligation), "EffectiveResolutionHeadDecisionId")]
    public void ExactDecisionHeadsArePersistedWithRestrictDecisionForeignKeys(Type type, string property)
    {
        using var db = Context(); var entity = db.Model.FindEntityType(type)!;
        Assert.NotNull(entity.FindProperty(property));
        var foreignKey = Assert.Single(entity.GetForeignKeys().Where(key => key.Properties.Any(value => value.Name == property)));
        Assert.Equal(typeof(RepairDecision), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
    [Theory]
    [InlineData("0.0004", "10", "0", "3")]
    [InlineData("0", "10.0004", "0", "3")]
    [InlineData("0", "10", "0.0004", "3")]
    [InlineData("0", "1000000000000000", "0", "3")]
    public void ScopeAdmissionRejectsBoundsThatCannotPersistExactly(string from, string to, string offsetFrom, string offsetTo)
    {
        static decimal Value(string text) => decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentException>(() => RepairActualScope.Create(Guid.NewGuid(), Guid.NewGuid(), "source-frame", "road",
            Value(from), Value(to), Value(offsetFrom), Value(offsetTo)));
    }
    [Theory]
    [InlineData(typeof(RepairPolicyDraft), "RepairPolicyDrafts")]
    [InlineData(typeof(RepairPolicyRevision), "RepairPolicyRevisions")]
    [InlineData(typeof(RepairExecutionAuthorization), "RepairExecutionAuthorizations")]
    [InlineData(typeof(TemporarySafetyMeasure), "RepairTemporarySafetyMeasures")]
    public void PolicyGrantAndSafetySourcesHaveTypedDurablePersistence(Type type, string table)
    {
        using var db = Context(); var entity = db.Model.FindEntityType(type); Assert.NotNull(entity);
        Assert.Equal(table, entity.GetTableName());
        Assert.All(entity.GetForeignKeys(), foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }
    [Theory]
    [InlineData("CurrentChangeId")]
    [InlineData("PublishedRevisionId")]
    public void DraftUsesExplicitDurableChangeAndPublicationPins(string property)
    {
        using var db = Context(); var entity = db.Model.FindEntityType(typeof(RepairPolicyDraft)); Assert.NotNull(entity);
        Assert.NotNull(entity.FindProperty(property));
    }
}
