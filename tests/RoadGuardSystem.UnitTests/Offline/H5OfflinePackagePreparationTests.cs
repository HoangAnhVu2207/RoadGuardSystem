using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflinePackagePreparationTests
{
    [Fact]
    public void SupervisorRegistrationRecordsActualRegistrantSeparatelyFromImmutableSource()
    {
        var original = Guid.NewGuid(); var supervisor = Guid.NewGuid(); var registered = DateTimeOffset.UtcNow;
        var row = Register(original, supervisor, UserRoleCode.Supervisor, registered);
        Assert.Equal(original, row.OriginalActorId); Assert.Equal(supervisor, row.RegisteredBy);
        Assert.Equal(registered, row.RegisteredAt); Assert.Equal("SUPERVISOR_PREPARED", row.RegistrationMode);
        Assert.DoesNotContain(typeof(OfflineEncryptedPackageRecord).GetProperties(), property =>
            property.Name is "ExecutionAuthorized" or "TimeVerified" or "OriginalStartedAt");
    }

    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.RepairCrew)]
    public void OrdinaryReceiverCannotPrepareAnotherActorsUnregisteredPackage(UserRoleCode role)
        => Assert.Throws<ArgumentException>(() => Register(Guid.NewGuid(), Guid.NewGuid(), role, DateTimeOffset.UtcNow));

    private static OfflineEncryptedPackageRecord Register(Guid original, Guid registrant, UserRoleCode role, DateTimeOffset at)
        => OfflineEncryptedPackageRecord.RegisterPrepared(Guid.NewGuid(), Guid.NewGuid(), original, Guid.NewGuid(),
            Guid.NewGuid(), registrant, role, "{\"ciphertext\":\"sealed\"}", "{}", Convert.ToBase64String(new byte[64]),
            new string('a', 64), new string('b', 64), at);
}
