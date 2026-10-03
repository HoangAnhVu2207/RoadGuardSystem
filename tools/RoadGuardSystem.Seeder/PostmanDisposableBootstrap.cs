using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Seeder;

// Owner-authorized disposable database only. Ordinary Seeder behavior is unchanged.
internal static class PostmanDisposableBootstrap
{
    public static async Task<int> RunAsync(string[] args, Func<string, string?> env, CancellationToken ct)
    {
        var verifyOnly = args.Length == 2 && args[1] == "--verify-only";
        var addDemoMember = args.Length == 3 && args[1] == "--demo-project" && Guid.TryParse(args[2], out _);
        if (!(args.Length == 1 || verifyOnly || addDemoMember || (args.Length == 2 && args[1] == "--recreate"))) return 1;
        try
        {
            var target = new SqlConnectionStringBuilder(env(Program.ConnectionStringEnvVarName));
            if (!string.Equals(target.DataSource, @".\HANHNAV", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(target.InitialCatalog, "RoadGuardPostmanTest", StringComparison.Ordinal))
                throw new InvalidOperationException("Disposable target mismatch");
            var master = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };
            await using (var connection = new SqlConnection(master.ConnectionString))
            {
                await connection.OpenAsync(ct);
                await VerifyAsync(connection, "master", ct);
                if (args.Length == 2 && args[1] == "--recreate")
                {
                    await using var reset = connection.CreateCommand();
                    // DROP fails if another session is using the database; never kill it.
                    reset.CommandText = "IF DB_ID(N'RoadGuardPostmanTest') IS NOT NULL DROP DATABASE [RoadGuardPostmanTest]";
                    await reset.ExecuteNonQueryAsync(ct);
                    Console.WriteLine("Recreated target requested: previous disposable DB dropped; no storage deletion.");
                }
            }
            if (verifyOnly)
            {
                await using var connection = new SqlConnection(target.ConnectionString);
                await connection.OpenAsync(ct);
                await VerifyAsync(connection, "RoadGuardPostmanTest", ct);
                Console.WriteLine("Verified effective API target: .\\HANHNAV / RoadGuardPostmanTest");
                return 0;
            }
            await using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(target.ConnectionString, sql => sql.UseNetTopologySuite()).Options);
            // Verify the live instance immediately before Migrate (database may not yet exist).
            await using (var connection = new SqlConnection(master.ConnectionString))
            { await connection.OpenAsync(ct); await VerifyAsync(connection, "master", ct); }
            Console.WriteLine("Effective migration target: .\\HANHNAV / RoadGuardPostmanTest");
            await db.Database.MigrateAsync(ct);
            await db.Database.OpenConnectionAsync(ct);
            await VerifyAsync((SqlConnection)db.Database.GetDbConnection(), "RoadGuardPostmanTest", ct);
            await new DatabaseSeeder([new IdentityRoleSeedStep(), new DroneDeviceSeedStep(),
                new PostmanUserSeedStep(), new PostmanScenarioSeedStep()]).SeedAsync(db, ct);
            var reporterId = Guid.Parse("31926888-7144-490c-888a-63149b36a990");
            if (!await db.Users.AnyAsync(u => u.Id == reporterId, ct))
            {
                var user = new ApplicationUser { Id = reporterId, UserName = "reporter.runtime@example.test",
                    NormalizedUserName = "REPORTER.RUNTIME@EXAMPLE.TEST", Email = "reporter.runtime@example.test",
                    NormalizedEmail = "REPORTER.RUNTIME@EXAMPLE.TEST", EmailConfirmed = true,
                    DisplayName = "Synthetic Postman Reporter", RoleCode = UserRoleCode.Reporter,
                    Status = UserStatus.Active, MustChangePassword = false, SecurityStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTimeOffset.UtcNow };
                user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, PostmanUserSeedStep.StandardPassword);
                db.Users.Add(user);
            }
            var modelId = Guid.Parse("c8bcb98c-7407-4b2c-b0dc-0b9144d3dd62");
            if (!await db.AIModelVersions.AnyAsync(m => m.Id == modelId, ct))
                db.AIModelVersions.Add(AIModelVersion.Create(modelId, "Synthetic Postman mock model",
                    "synthetic-road-v1", "fixture://local", null, null, AIModelVersionStatus.Released,
                    DateTimeOffset.UtcNow, PostmanUserSeedStep.SupervisorUserId));
            if (!await db.DefectTypes.AnyAsync(t => t.Code == "CRACK", ct))
                db.DefectTypes.Add(DefectType.Create("CRACK", "Synthetic mock crack", "Demo only"));
            await db.SaveChangesAsync(ct);
            foreach (var (id, role, email, password) in new[] {
                (PostmanUserSeedStep.SupervisorUserId, UserRoleCode.Supervisor, PostmanUserSeedStep.SupervisorEmail, PostmanUserSeedStep.SupervisorPassword),
                (PostmanUserSeedStep.ProjectManagerUserId, UserRoleCode.ProjectManager, PostmanUserSeedStep.ProjectManagerEmail, PostmanUserSeedStep.StandardPassword),
                (PostmanUserSeedStep.OperatorUserId, UserRoleCode.DroneOperator, PostmanUserSeedStep.OperatorEmail, PostmanUserSeedStep.StandardPassword),
                (PostmanUserSeedStep.RepairCrewUserId, UserRoleCode.RepairCrew, PostmanUserSeedStep.RepairCrewEmail, PostmanUserSeedStep.StandardPassword),
                (reporterId, UserRoleCode.Reporter, "reporter.runtime@example.test", PostmanUserSeedStep.StandardPassword) })
            {
                var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == id, ct);
                if (user.Email != email || user.RoleCode != role || user.Status != UserStatus.Active || user.MustChangePassword
                    || user.PasswordHash is null || new PasswordHasher<ApplicationUser>().VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed
                    || !await db.Roles.AnyAsync(r => r.Code == role && r.IsActive, ct))
                    throw new InvalidOperationException("Synthetic fixture authority/credential mismatch; explicit disposable recreate required");
            }
            if (addDemoMember)
            {
                var pid = Guid.Parse(args[2]);
                if (!await db.Projects.AnyAsync(p => p.Id == pid && p.ProjectCode == "DEMO-ANH01-20261002", ct))
                    throw new InvalidOperationException("Demo project ownership mismatch");
                if (!await db.ProjectMembers.AnyAsync(m => m.ProjectId == pid && m.UserId == PostmanUserSeedStep.OperatorUserId, ct))
                {
                    db.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = pid,
                        UserId = PostmanUserSeedStep.OperatorUserId, RoleCode = UserRoleCode.DroneOperator,
                        IsPrimary = false, ValidFrom = new DateOnly(2020, 1, 1), Status = ProjectMemberStatus.Active });
                    await db.SaveChangesAsync(ct);
                }
            }
            Console.WriteLine("SUCCESS: production migrations and repeatable synthetic fixtures; no seeded files/VERIFIED objects or Huy approvals.");
            return 0;
        }
        catch (Exception ex)
        {
            // Connection/provider exceptions can contain credentials; emit only the type.
            Console.Error.WriteLine("Disposable bootstrap failed: " + ex.GetType().Name);
            return 3;
        }
    }

    private static async Task VerifyAsync(SqlConnection connection, string database, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')), DB_NAME()";
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct) || !string.Equals(reader.GetString(0), "HANHNAV", StringComparison.OrdinalIgnoreCase)
            || reader.GetString(1) != database) throw new InvalidOperationException("Live SQL target mismatch");
    }
}
