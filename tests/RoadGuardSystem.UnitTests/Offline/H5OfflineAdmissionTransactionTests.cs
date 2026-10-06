using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Implementations.Offline;
using RoadGuardSystem.Repositories.Inspections;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineAdmissionTransactionTests
{
    [Fact]
    public async Task ImportedFieldAdmissionCannotRunOutsideActualCallerTransaction()
    {
        using var context = Context(); var validator = new OfflineAdmissionValidator(context, TimeProvider.System);
        var command = new FieldWorkflowCommand(Guid.NewGuid(), Guid.NewGuid(), "start", null, null, null,
            new(Guid.NewGuid(), UserRoleCode.RepairCrew, Guid.NewGuid(), "HANDOVER", false, Guid.NewGuid(), Guid.NewGuid()));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => validator.ValidateAsync(command, CancellationToken.None));
        Assert.Equal("Caller-owned offline transaction required.", error.Message);
    }

    [Fact]
    public async Task DirectCanonicalBindingGuardCannotRunOutsideActualCallerTransaction()
    {
        using var context = Context(); var validator = new OfflineAdmissionValidator(context, TimeProvider.System);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => validator.GuardOriginBindingAsync(Guid.NewGuid(),
            Guid.NewGuid(), "FIELD_START", new string('a', 64), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
        Assert.Equal("Caller-owned offline transaction required.", error.Message);
    }

    private static RoadGuardDbContext Context() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer("Server=(local);Database=H5_NoConnection_TransactionBoundary;Integrated Security=true;TrustServerCertificate=true",
            sql => sql.UseNetTopologySuite())
        .Options);
}
