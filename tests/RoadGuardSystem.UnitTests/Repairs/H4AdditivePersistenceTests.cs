using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4AdditivePersistenceTests
{
    [Theory]
    [InlineData(typeof(FieldInspectionTask), "RepairItemId")]
    [InlineData(typeof(RepairItem), "CurrentBindingId")]
    [InlineData(typeof(RepairItem), "CurrentAttemptId")]
    [InlineData(typeof(RepairItem), "EffectiveIntakeSubmissionId")]
    [InlineData(typeof(RepairItem), "CurrentReviewId")]
    [InlineData(typeof(RepairItem), "PredecessorItemId")]
    [InlineData(typeof(RepairItem), "SupersededByItemId")]
    [InlineData(typeof(RepairObligation), "CurrentRepairItemId")]
    [InlineData(typeof(RepairObligation), "OriginalCrewFirstStartId")]
    [InlineData(typeof(RepairDecision), "PreviousObligationHeadDecisionId")]
    [InlineData(typeof(RepairNormalSuccessor), "SourceCancellationEventId")]
    [InlineData(typeof(RepairNormalSuccessor), "SourceHandoverEventId")]
    public void RuntimePinsAreTypedNullableRestrictForeignKeys(Type type, string property)
    {
        using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(sql => sql.UseNetTopologySuite()).Options);
        var entity = db.Model.FindEntityType(type)!;
        Assert.NotNull(type.GetProperty(property));
        var pin = entity.FindProperty(property); Assert.NotNull(pin); Assert.True(pin.IsNullable);
        var key = Assert.Single(entity.GetForeignKeys().Where(key => key.Properties.Any(value => value.Name == property)));
        Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior);
    }

    [Theory]
    [InlineData("RepairFieldTaskBindings")]
    [InlineData("RepairMeasurementAssessments")]
    [InlineData("RepairExecutionStarts")]
    [InlineData("RepairExecutionFinishes")]
    [InlineData("RepairAttemptSubmissionLinks")]
    [InlineData("RepairAttemptReviews")]
    [InlineData("RepairItemLifecycleEvents")]
    [InlineData("RepairNormalSuccessors")]
    public void NewProducerSourcesHaveDedicatedAppendOnlyTables(string table)
    {
        using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(sql => sql.UseNetTopologySuite()).Options);
        var entity = Assert.Single(db.Model.GetEntityTypes().Where(entity => entity.GetTableName() == table));
        Assert.NotNull(entity.FindPrimaryKey());
        Assert.Contains(entity.GetDeclaredTriggers(), trigger => trigger.ModelName.EndsWith("_Immutable", StringComparison.Ordinal));
        Assert.All(entity.GetForeignKeys(), key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));
    }
}
