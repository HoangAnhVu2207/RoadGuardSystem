using Microsoft.EntityFrameworkCore;

namespace RoadGuardSystem.Repositories.Idempotency;

// ANH-02 only: lock current authority in a fixed order through the caller's scoped context.
// The receipt service owns the transaction and keeps these locks through outcome mapping/commit.
internal static class Anh02ReceiptAuthority
{
    internal static async Task LockAsync(RoadGuardDbContext db, Guid actor, Guid? project, CancellationToken token)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Receipt authority requires the service-owned transaction.");
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={actor}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (user is not null)
        {
            var code = RoadGuardSystem.aBusinessObjects.Commons.UserRoleCodeExtensions.ToDbCode(user.RoleCode);
            await db.Roles.FromSqlInterpolated($"SELECT * FROM [Roles] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]={code}").AsNoTracking().ToListAsync(token);
        }
        if (project is { } id)
        {
            await db.Projects.FromSqlInterpolated($"SELECT * FROM [Projects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").AsNoTracking().ToListAsync(token);
            await db.ProjectMembers.FromSqlInterpolated($"SELECT * FROM [ProjectMembers] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={id} AND [UserId]={actor} ORDER BY [Id]")
                .AsNoTracking().ToListAsync(token);
        }
    }
}
