using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Repairs;

/// <summary>Exact obligation authority overlay. Business graph project IDs remain immutable provenance.</summary>
public static class ObligationResponsibilityScope
{
    public static async Task<Guid> ResolveAsync(RoadGuardDbContext db, Guid obligationId,
        Guid originProjectId, CancellationToken token)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Responsibility resolution requires the business transaction.");
        var owner = await db.Set<ObligationResponsibility>().FromSqlInterpolated(
            $"SELECT * FROM [ObligationResponsibilities] WITH (UPDLOCK,HOLDLOCK) WHERE [ObligationId]={obligationId}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (owner is null) return originProjectId;
        if (owner.OriginProjectId != originProjectId)
            throw new InvalidOperationException("Responsibility origin does not match the obligation graph.");
        return owner.CurrentProjectId;
    }
}
