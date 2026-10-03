using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

public sealed class Huy01RoleAuthoritySqlTests(AuthenticationSqlServerFixture fixture)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task InactiveProjectManagerRoleDeniesCaseGuardBeforeResourceRead()
    {
        var manager = await fixture.CreateUserAsync($"case-role-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using (var db = fixture.CreateDbContext())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Roles] SET [IsActive]={false} WHERE [Code]={UserRoleCode.ProjectManager.ToDbCode()}");
        try
        {
            await using var db = fixture.CreateDbContext();
            var guard = new CaseWorkflowRepository(db);
            var error = await Assert.ThrowsAsync<CaseWorkflowException>(() => guard.GuardAsync(
                manager.Id, UserRoleCode.ProjectManager, [], null, (_, _) => Task.FromResult(true), default));
            Assert.Equal(403, error.Status);
            Assert.Equal("access_forbidden", error.Code);
        }
        finally
        {
            await using var db = fixture.CreateDbContext();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Roles] SET [IsActive]={true} WHERE [Code]={UserRoleCode.ProjectManager.ToDbCode()}");
        }
    }
}
