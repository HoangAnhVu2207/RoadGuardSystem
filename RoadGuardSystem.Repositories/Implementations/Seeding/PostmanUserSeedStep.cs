using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>
/// Creates deterministic, non-production accounts for local Postman/API smoke testing.
/// This step is registered only by the Development composition root.
/// </summary>
public sealed class PostmanUserSeedStep : ISeedStep
{
    public const string SupervisorEmail = "supervisor.postman@example.test";
    public const string ProjectManagerEmail = "pm.postman@example.test";
    public const string OperatorEmail = "operator.postman@example.test";
    public const string RepairCrewEmail = "crew.postman@example.test";
    public const string SupervisorPassword = "Supervisor1!";
    public const string StandardPassword = "Current1!";

    private static readonly IReadOnlyList<FixtureUser> Users =
    [
        new("4c3d3e4d-3f6a-4b13-b4d5-6ef7e47d1a01", SupervisorEmail, "Postman Supervisor", UserRoleCode.Supervisor, SupervisorPassword),
        new("4c3d3e4d-3f6a-4b13-b4d5-6ef7e47d1a02", ProjectManagerEmail, "Postman Project Manager", UserRoleCode.ProjectManager, StandardPassword),
        new("4c3d3e4d-3f6a-4b13-b4d5-6ef7e47d1a03", OperatorEmail, "Postman Drone Operator", UserRoleCode.DroneOperator, StandardPassword),
        new("4c3d3e4d-3f6a-4b13-b4d5-6ef7e47d1a04", RepairCrewEmail, "Postman Repair Crew", UserRoleCode.RepairCrew, StandardPassword)
    ];

    public int Order => 30;

    public string Name => nameof(PostmanUserSeedStep);

    public async Task SeedAsync(RoadGuardDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var hasher = new PasswordHasher<ApplicationUser>();

        foreach (var fixture in Users)
        {
            var normalizedEmail = fixture.Email.ToUpperInvariant();
            var existing = await context.Users.SingleOrDefaultAsync(
                    user => user.Email == fixture.Email || user.NormalizedEmail == normalizedEmail,
                    cancellationToken);
            if (existing is not null)
            {
                if (!string.IsNullOrWhiteSpace(existing.PasswordHash) &&
                    hasher.VerifyHashedPassword(existing, existing.PasswordHash, fixture.Password) == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    existing.PasswordHash = hasher.HashPassword(existing, fixture.Password);
                    await context.SaveChangesAsync(cancellationToken);
                }

                continue;
            }

            var user = new ApplicationUser
            {
                Id = Guid.Parse(fixture.Id),
                UserName = fixture.Email,
                NormalizedUserName = normalizedEmail,
                Email = fixture.Email,
                NormalizedEmail = normalizedEmail,
                EmailConfirmed = true,
                DisplayName = fixture.DisplayName,
                RoleCode = fixture.RoleCode,
                Status = UserStatus.Active,
                MustChangePassword = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTimeOffset.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, fixture.Password);
            context.Users.Add(user);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                context.ChangeTracker.Clear();
                if (!await context.Users.AnyAsync(user => user.Email == fixture.Email, cancellationToken))
                {
                    throw;
                }
            }
        }
    }

    private sealed record FixtureUser(
        string Id,
        string Email,
        string DisplayName,
        UserRoleCode RoleCode,
        string Password);
}
