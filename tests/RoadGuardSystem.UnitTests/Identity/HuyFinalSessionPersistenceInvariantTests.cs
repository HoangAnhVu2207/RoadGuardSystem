using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.UnitTests.Identity;

public sealed class HuyFinalSessionPersistenceInvariantTests
{
    [Fact]
    public void BoundedSessionWithoutExpiry_IsRejectedBeforeAnyDatabaseCall()
    {
        using var db = Db();
        db.Add(new UserSession { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IssuedAt = DateTimeOffset.UtcNow });
        var error = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Contains("Session lifecycle", error.Message);
    }

    [Theory]
    [InlineData("lifecycle")]
    [InlineData("role")]
    public void ExistingSessionLifecycleAndIssuedRole_AreWriteOnce(string field)
    {
        using var db = Db();
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            IssuedRole = UserRoleCode.ProjectManager
        };
        db.Attach(session);
        if (field == "lifecycle") session.Lifecycle = SessionLifecycle.PersistentRenewable;
        else session.IssuedRole = UserRoleCode.RepairCrew;
        var error = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Contains("write-once", error.Message);
    }

    private static RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(sql => sql.UseNetTopologySuite()).Options);
}
