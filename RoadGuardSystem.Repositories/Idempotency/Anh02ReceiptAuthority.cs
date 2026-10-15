using Microsoft.EntityFrameworkCore;

namespace RoadGuardSystem.Repositories.Idempotency;

// ANH-02 only: lock current authority in a fixed order through the caller's scoped context.
// The receipt service owns the transaction and keeps these locks through outcome mapping/commit.
internal static class Anh02ReceiptAuthority
{
    internal static async Task LockAsync(RoadGuardDbContext db, Guid actor, Guid? project, CancellationToken token,
        bool readOnly = false)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Receipt authority requires the service-owned transaction.");
        // Notification source proof already owns business-resource locks. It only reads
        // authority: retained S/range locks block revocation without competing with the
        // U locks held by authority-first commands. Commands retain the existing default.
        var user = await db.Users.FromSqlInterpolated(readOnly
                ? (FormattableString)$"SELECT * FROM [Users] WITH (HOLDLOCK) WHERE [Id]={actor}"
                : $"SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={actor}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (user is not null)
        {
            var code = RoadGuardSystem.aBusinessObjects.Commons.UserRoleCodeExtensions.ToDbCode(user.RoleCode);
            await db.Roles.FromSqlInterpolated(readOnly
                ? (FormattableString)$"SELECT * FROM [Roles] WITH (HOLDLOCK) WHERE [Code]={code}"
                : $"SELECT * FROM [Roles] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]={code}").AsNoTracking().ToListAsync(token);
        }
        if (project is { } id)
        {
            await db.Projects.FromSqlInterpolated(readOnly
                ? (FormattableString)$"SELECT * FROM [Projects] WITH (HOLDLOCK) WHERE [Id]={id}"
                : $"SELECT * FROM [Projects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").AsNoTracking().ToListAsync(token);
            await db.ProjectMembers.FromSqlInterpolated(readOnly
                ? (FormattableString)$"SELECT * FROM [ProjectMembers] WITH (HOLDLOCK) WHERE [ProjectId]={id} AND [UserId]={actor} ORDER BY [Id]"
                : $"SELECT * FROM [ProjectMembers] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={id} AND [UserId]={actor} ORDER BY [Id]")
                .AsNoTracking().ToListAsync(token);
        }
    }
}
