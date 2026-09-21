using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>
/// Idempotent seed step that provisions the four canonical roles:
/// SUPERVISOR, PM, DRONE_OPERATOR, and REPAIR_CREW.
/// </summary>
public sealed class IdentityRoleSeedStep : ISeedStep
{
    public int Order => 10;

    public string Name => "IdentityRoleSeedStep";

    public static readonly IReadOnlyList<(string Code, string Name)> CanonicalRoles = new List<(string, string)>
    {
        (UserRoleCodeExtensions.SupervisorDbCode, "Supervisor"),
        (UserRoleCodeExtensions.ProjectManagerDbCode, "Project Manager"),
        (UserRoleCodeExtensions.DroneOperatorDbCode, "Drone Operator"),
        (UserRoleCodeExtensions.RepairCrewDbCode, "Repair Crew")
    };

    public async Task SeedAsync(RoadGuardDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var existingList = await context.Roles
            .AsNoTracking()
            .Select(r => r.Code)
            .ToListAsync(cancellationToken);
        var existingCodes = existingList.ToHashSet();

        var missingRoles = CanonicalRoles
            .Where(r => !existingCodes.Contains(UserRoleCodeExtensions.FromDbCode(r.Code)))
            .ToList();

        if (missingRoles.Count == 0)
        {
            return;
        }

        foreach (var (codeStr, name) in missingRoles)
        {
            var codeEnum = UserRoleCodeExtensions.FromDbCode(codeStr);
            try
            {
                var newRole = new ApplicationRole(codeEnum, name)
                {
                    IsActive = true
                };
                context.Roles.Add(newRole);
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A concurrent seeder already inserted this role.
                // Clear the failed entity from change tracker and verify the role exists in the database.
                context.ChangeTracker.Clear();
                var exists = await context.Roles.AnyAsync(r => r.Code == codeEnum, cancellationToken);
                if (!exists)
                {
                    throw;
                }
            }
        }
    }
}
