using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflinePersistenceModelTests
{
    public static TheoryData<Type, string> RetainedTypes => new()
    {
        { typeof(OfflineDeviceRegistration), "OfflineDeviceRegistrations" },
        { typeof(OfflineDeviceRevocation), "OfflineDeviceRevocations" },
        { typeof(OfflineTaskSnapshot), "OfflineTaskSnapshots" },
        { typeof(OfflineOperationBinding), "OfflineOperationBindings" },
        { typeof(OfflineEncryptedPackageRecord), "OfflineEncryptedPackages" },
        { typeof(OfflineHandoverGrant), "OfflineHandoverGrants" },
        { typeof(OfflineHandoverGrantRevocation), "OfflineHandoverGrantRevocations" },
        { typeof(OfflineSyncBatch), "OfflineSyncBatches" },
        { typeof(OfflineOperationAdmission), "OfflineOperationAdmissions" },
        { typeof(OfflineOperationResult), "OfflineOperationResults" },
        { typeof(OfflineEvidenceCaptureReference), "OfflineEvidenceCaptureReferences" },
        { typeof(OfflinePackageFileReference), "OfflinePackageFileReferences" },
        { typeof(OfflineAdmittedFileReference), "OfflineAdmittedFileReferences" },
        { typeof(OfflineEncryptedCaptureArtifact), "OfflineEncryptedCaptureArtifacts" },
        { typeof(OfflineOriginTimeVerification), "OfflineOriginTimeVerifications" }
    };

    [Theory]
    [MemberData(nameof(RetainedTypes))]
    public void Actual_model_retains_typed_offline_history_with_restrict_scope_and_immutable_guards(Type type, string table)
    {
        using var context = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(sql => sql.UseNetTopologySuite()).Options);
        var entity = context.Model.FindEntityType(type);
        Assert.NotNull(entity);
        Assert.Equal(table, entity.GetTableName());
        Assert.Contains(entity.GetKeys(), key => key.Properties.Select(property => property.Name)
            .SequenceEqual(new[] { "Id", "ProjectId" }));
        Assert.All(entity.GetForeignKeys(), foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Contains(entity.GetDeclaredTriggers(), trigger => trigger.ModelName == $"TR_{table}_Immutable");
        Assert.Contains(entity.GetDeclaredTriggers(), trigger => trigger.ModelName == $"TR_{table}_Scope");
    }
}
