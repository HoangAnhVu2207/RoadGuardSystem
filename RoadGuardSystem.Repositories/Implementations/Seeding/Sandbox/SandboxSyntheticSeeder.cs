using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Seeding.Sandbox;

public sealed class SandboxSyntheticSeeder
{
    public static async Task<SandboxSeedOutcome> InitializeAsync(RoadGuardDbContext db, SandboxSeedTarget target,
        SandboxSeedCatalog catalog, SandboxCredentialInput credentials, CancellationToken ct = default)
    {
        catalog.Validate();
        if (credentials.BundleId == Guid.Empty || credentials.SeedVersion != catalog.Version ||
            !credentials.Passwords.Keys.Order().SequenceEqual(catalog.Actors.Select(a => a.Id).Order()) ||
            credentials.Passwords.Values.Any(p => p.Length < 44) || credentials.Passwords.Values.Distinct().Count() != catalog.Actors.Length)
            throw new InvalidOperationException("CredentialMismatch");
        await SandboxSeedGuard.ValidateTargetAsync(db, target, ct);
        var credentialDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(credentials.Passwords.OrderBy(p => p.Key)))));
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(catalog))));
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var locked = await SandboxSeedGuard.ScalarAsync(db, "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'RoadGuardSandboxSeed',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=30000; SELECT @r;", ct);
            if (!int.TryParse(locked, out var code) || code < 0) throw new InvalidOperationException("LockTimeout");
            await SandboxSeedGuard.ValidateTargetAsync(db, target, ct);
            var schema = await SandboxSeedGuard.ReadSchemaAsync(db, ct);
            if (schema.Signature != catalog.Schema.Signature)
                throw new InvalidOperationException($"SchemaMismatch: structure signature {schema.Signature}");
            if (!schema.Tables.SequenceEqual(catalog.Schema.Tables))
                throw new InvalidOperationException("SchemaMismatch: table catalog order or membership");
            if (!schema.Migrations.SequenceEqual(catalog.Schema.Migrations))
                throw new InvalidOperationException("SchemaMismatch: migration catalog");
            var expected = new Marker(catalog.Version, digest, schema.Signature, credentials.BundleId, credentialDigest);
            var marker = await SandboxSeedGuard.ScalarAsync(db, "SELECT CONVERT(nvarchar(4000),value) FROM sys.extended_properties WHERE class=0 AND name=N'RoadGuardSandboxSeed';", ct);
            if (marker is not null)
            {
                Marker? existing;
                try { existing = JsonSerializer.Deserialize<Marker>(marker); } catch (JsonException) { throw new InvalidOperationException("VersionMismatch"); }
                if (existing is not null && existing.BundleId == expected.BundleId && existing.CredentialDigest != expected.CredentialDigest) throw new InvalidOperationException("CredentialMismatch");
                if (existing != expected) throw new InvalidOperationException("VersionMismatch");
                await tx.CommitAsync(ct);
                return new SandboxSeedOutcome(false, catalog.Version, credentials.BundleId, 11, 2);
            }
            await LockAndAssertPristineAsync(db, schema, ct);
            await InsertCoreAsync(db, catalog, credentials, ct);
            var value = JsonSerializer.Serialize(expected);
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_addextendedproperty @name=N'RoadGuardSandboxSeed', @value={value};", ct);
            await SandboxSeedGuard.ValidateTargetAsync(db, target, ct);
            if ((await SandboxSeedGuard.ReadSchemaAsync(db, ct)).Signature != schema.Signature) throw new InvalidOperationException("SchemaMismatch");
            await tx.CommitAsync(ct);
            return new SandboxSeedOutcome(true, catalog.Version, credentials.BundleId, 11, 2);
        });
    }

    private static async Task LockAndAssertPristineAsync(RoadGuardDbContext db, SandboxSchemaFacts schema, CancellationToken ct)
    {
        foreach (var table in db.Model.GetRelationalModel().Tables.OrderBy(t => (t.Schema ?? "dbo") + "." + t.Name, StringComparer.Ordinal))
        {
            static string Quote(string name) => "[" + name.Replace("]", "]]", StringComparison.Ordinal) + "]";
            var name = Quote(table.Schema ?? "dbo") + "." + Quote(table.Name);
            var count = await SandboxSeedGuard.ScalarAsync(db, $"SELECT COUNT_BIG(*) FROM {name} WITH (TABLOCKX,HOLDLOCK);", ct);
            if (table.Name is not "Roles" and not "DroneDevices" && count != "0") throw new InvalidOperationException("NonPristine");
        }
        var roles = await db.Roles.AsNoTracking().ToListAsync(ct);
        // IdentityRoleSeedStep persists stamps through RoadGuardDbContext. They are opaque
        // concurrency metadata, not role identity; recognize only the exact canonical baseline.
        if (roles.Count != 0 && (roles.Count != 5 || IdentityRoleSeedStep.CanonicalRoles.Any(r =>
            !roles.Any(a => a.Code == UserRoleCodeExtensions.FromDbCode(r.Code) && a.Name == r.Name && string.Equals(a.NormalizedName, r.Name.ToUpperInvariant(), StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(a.ConcurrencyStamp) && a.IsActive))))
            throw new InvalidOperationException("NonPristine");
        var devices = await db.DroneDevices.AsNoTracking().ToListAsync(ct);
        if (devices.Count != 0 && (devices.Count != 1 || devices[0].Id != DroneDeviceSeedStep.FixtureDeviceId ||
            devices[0].SerialNo != DroneDeviceSeedStep.FixtureSerialNo || devices[0].Status != DroneDeviceStatus.Active ||
            devices[0].Model != "RoadGuard fixture device" || devices[0].ChecklistVersion != "v1"))
            throw new InvalidOperationException("NonPristine");
    }

    private static async Task InsertCoreAsync(RoadGuardDbContext db, SandboxSeedCatalog catalog, SandboxCredentialInput credentials, CancellationToken ct)
    {
        if (!await db.Roles.AnyAsync(ct))
            foreach (var role in IdentityRoleSeedStep.CanonicalRoles) db.Roles.Add(new ApplicationRole(UserRoleCodeExtensions.FromDbCode(role.Code), role.Name) { IsActive = true });
        if (!await db.DroneDevices.AnyAsync(ct)) db.DroneDevices.Add(RoadGuardSystem.BusinessObjects.Devices.DroneDevice.Create(
            DroneDeviceSeedStep.FixtureDeviceId, DroneDeviceSeedStep.FixtureSerialNo, DroneDeviceStatus.Active, "RoadGuard fixture device", "v1"));
        var now = DateTimeOffset.UtcNow;
        var hasher = new PasswordHasher<ApplicationUser>();
        foreach (var actor in catalog.Actors)
        {
            var user = new ApplicationUser
            {
                Id = actor.Id,
                UserName = actor.Email,
                NormalizedUserName = actor.Email.ToUpperInvariant(),
                Email = actor.Email,
                NormalizedEmail = actor.Email.ToUpperInvariant(),
                EmailConfirmed = true,
                DisplayName = "Synthetic " + actor.Key,
                RoleCode = actor.Role,
                Status = actor.Suspended ? UserStatus.Suspended : UserStatus.Active,
                SuspendedAt = actor.Suspended ? now : null,
                MustChangePassword = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAt = now
            };
            user.PasswordHash = hasher.HashPassword(user, credentials.Passwords[actor.Id]);
            db.Users.Add(user);
        }
        foreach (var key in new[] { "A", "B" })
            db.Projects.Add(Project.Create(key == "A" ? SandboxFixtureIds.ProjectA : SandboxFixtureIds.ProjectB, "SBX-" + key, "Synthetic Project " + key,
                "Synthetic sandbox; not operational evidence", null, null, null, now));
        foreach (var actor in catalog.Actors.Where(a => a.Project is not null))
        {
            var id = Guid.Parse(actor.Id.ToString().Replace("a001", "b001", StringComparison.Ordinal));
            var project = actor.Project == "A" ? SandboxFixtureIds.ProjectA : SandboxFixtureIds.ProjectB;
            db.ProjectMembers.Add(actor.Role == UserRoleCode.ProjectManager
                ? ProjectMember.CreatePrimaryProjectManager(id, project, actor.Id, new DateOnly(2000, 1, 1))
                : new ProjectMember
                {
                    Id = id,
                    ProjectId = project,
                    UserId = actor.Id,
                    RoleCode = actor.Role,
                    ValidFrom = new DateOnly(2000, 1, 1),
                    ValidTo = actor.Ended ? new DateOnly(2001, 1, 1) : null,
                    Status = actor.Ended ? ProjectMemberStatus.Ended : ProjectMemberStatus.Active
                });
        }
        await db.SaveChangesAsync(ct);
    }
    private sealed record Marker(string Version, string CatalogDigest, string SchemaSignature, Guid BundleId, string CredentialDigest);
}
