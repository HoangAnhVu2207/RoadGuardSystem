using Microsoft.EntityFrameworkCore;

namespace RoadGuardSystem.Repositories.Idempotency;

// Multipart recovery uses the same scoped DbContext and ordered authority locks as
// the upload command. It only reads/locks facts; the caller owns the transaction.
internal static class MultipartReceiptAuthority
{
    internal static async Task LockAsync(RoadGuardDbContext db, Guid actor, Guid? project, CancellationToken token)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Multipart authority requires the service-owned transaction.");

        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={actor}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (user is not null)
        {
            var code = RoadGuardSystem.aBusinessObjects.Commons.UserRoleCodeExtensions.ToDbCode(user.RoleCode);
            await db.Roles.FromSqlInterpolated($"SELECT * FROM [Roles] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]={code}")
                .AsNoTracking().ToListAsync(token);
        }

        if (project is { } projectId)
        {
            await db.Projects.FromSqlInterpolated($"SELECT * FROM [Projects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={projectId}")
                .AsNoTracking().ToListAsync(token);
            await db.ProjectMembers.FromSqlInterpolated($"SELECT * FROM [ProjectMembers] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={projectId} AND [UserId]={actor} ORDER BY [Id]")
                .AsNoTracking().ToListAsync(token);
        }
    }
}
