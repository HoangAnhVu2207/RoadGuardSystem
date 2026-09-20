using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Projects;

[Trait("TaskId", "P2-20")]
public sealed class P220ProjectMembershipSchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P220ProjectMembershipSchemaTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-20: An active project cannot persist two active primary PM memberships")]
    public async Task ActiveProject_TwoActivePrimaryProjectManagers_AreRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"PRJ-{Guid.NewGuid():N}",
            Name = "National Route 1A rehabilitation",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var firstManager = CreateUser(UserRoleCode.ProjectManager);
        var secondManager = CreateUser(UserRoleCode.ProjectManager);
        context.Users.AddRange(firstManager, secondManager);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.ProjectMembers.AddRange(
            CreateActivePrimaryMembership(project.Id, firstManager.Id),
            CreateActivePrimaryMembership(project.Id, secondManager.Id));

        var persist = () => context.SaveChangesAsync();

        await persist.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-20: An active project membership rejects an inverted effective date range")]
    public async Task ProjectMember_ValidToBeforeValidFrom_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"PRJ-{Guid.NewGuid():N}",
            Name = "Provincial Route 8 resurfacing",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var operatorUser = CreateUser(UserRoleCode.DroneOperator);
        context.Users.Add(operatorUser);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = operatorUser.Id,
            RoleCode = UserRoleCode.DroneOperator,
            IsPrimary = false,
            ValidFrom = new DateOnly(2026, 9, 21),
            ValidTo = new DateOnly(2026, 9, 20),
            Status = ProjectMemberStatus.Active
        });

        var persist = () => context.SaveChangesAsync();

        await persist.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-20: A primary project membership must be assigned to a Project Manager")]
    public async Task ProjectMember_NonProjectManagerPrimaryMembership_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"PRJ-{Guid.NewGuid():N}",
            Name = "Coastal Road rehabilitation",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var operatorUser = CreateUser(UserRoleCode.DroneOperator);
        context.Users.Add(operatorUser);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = operatorUser.Id,
            RoleCode = UserRoleCode.DroneOperator,
            IsPrimary = true,
            ValidFrom = new DateOnly(2026, 9, 20),
            Status = ProjectMemberStatus.Active
        });

        var persist = () => context.SaveChangesAsync();

        await persist.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-20: A project cannot persist duplicate handover document numbers")]
    public async Task Project_DuplicateHandoverDocumentNumber_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"PRJ-{Guid.NewGuid():N}",
            Name = "Mountain Pass rehabilitation",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.HandoverDocuments.AddRange(
            new HandoverDocument
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                DocumentNo = "BBBG-2026-001",
                HandoverDate = new DateOnly(2026, 9, 20)
            },
            new HandoverDocument
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                DocumentNo = "BBBG-2026-001",
                HandoverDate = new DateOnly(2026, 9, 21)
            });

        var persist = () => context.SaveChangesAsync();

        await persist.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-20: A stale handover document update is rejected by rowversion")]
    public async Task HandoverDocument_StaleUpdate_IsRejectedBySqlServer()
    {
        var projectId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        await using (var setup = _fixture.CreateDbContext())
        {
            setup.Projects.Add(new Project
            {
                Id = projectId,
                ProjectCode = $"PRJ-{Guid.NewGuid():N}",
                Name = "Airport access road rehabilitation",
                Status = ProjectStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });
            setup.HandoverDocuments.Add(new HandoverDocument
            {
                Id = documentId,
                ProjectId = projectId,
                DocumentNo = "BBBG-2026-STALE",
                HandoverDate = new DateOnly(2026, 9, 20)
            });
            await setup.SaveChangesAsync();
        }

        await using var winner = _fixture.CreateDbContext();
        await using var stale = _fixture.CreateDbContext();
        var winnerDocument = await winner.HandoverDocuments.SingleAsync(document => document.Id == documentId);
        var staleDocument = await stale.HandoverDocuments.SingleAsync(document => document.Id == documentId);
        winnerDocument.Notes = "Accepted after final inspection.";
        await winner.SaveChangesAsync();

        staleDocument.Notes = "Stale competing update.";
        var persistStale = () => stale.SaveChangesAsync();

        await persistStale.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact(DisplayName = "P2-20: Membership fixture provides active scoped roles and an expired cross-project record")]
    public async Task ProjectMembershipFixture_CreatesP211ScopedAuthorizationData()
    {
        var data = await ProjectMembershipSqlFixture.CreateAsync(_fixture);
        await using var context = _fixture.CreateDbContext();

        var activeMembers = await context.ProjectMembers
            .Where(member => member.ProjectId == data.PrimaryProjectId && member.Status == ProjectMemberStatus.Active)
            .ToListAsync();

        activeMembers.Should().Contain(member => member.UserId == data.ProjectManagerId && member.IsPrimary);
        activeMembers.Should().Contain(member => member.UserId == data.DroneOperatorId && member.RoleCode == UserRoleCode.DroneOperator);
        activeMembers.Should().Contain(member => member.UserId == data.RepairCrewId && member.RoleCode == UserRoleCode.RepairCrew);
        activeMembers.Should().NotContain(member => member.UserId == data.ExpiredDroneOperatorId);
    }

    [Fact(DisplayName = "P2-20: Project membership and handover records reject invalid User, Project and File foreign keys")]
    public async Task ProjectHistory_InvalidUserProjectAndFileForeignKeys_AreRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var project = CreateProject();
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = Guid.NewGuid(),
            RoleCode = UserRoleCode.DroneOperator,
            IsPrimary = false,
            ValidFrom = new DateOnly(2026, 9, 20),
            Status = ProjectMemberStatus.Active
        });
        Func<Task> persistInvalidUser = () => context.SaveChangesAsync();
        await persistInvalidUser.Should().ThrowAsync<DbUpdateException>();
        context.ChangeTracker.Clear();

        context.HandoverDocuments.Add(new HandoverDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DocumentNo = "BBBG-FK-PROJECT",
            HandoverDate = new DateOnly(2026, 9, 20)
        });
        Func<Task> persistInvalidProject = () => context.SaveChangesAsync();
        await persistInvalidProject.Should().ThrowAsync<DbUpdateException>();
        context.ChangeTracker.Clear();

        context.HandoverDocuments.Add(new HandoverDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            FileId = Guid.NewGuid(),
            DocumentNo = "BBBG-FK-FILE",
            HandoverDate = new DateOnly(2026, 9, 20)
        });
        Func<Task> persistInvalidFile = () => context.SaveChangesAsync();
        await persistInvalidFile.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-20: A project referenced by handover history cannot be hard deleted")]
    public async Task Project_ReferencedByHandoverHistory_CannotBeHardDeleted()
    {
        await using var context = _fixture.CreateDbContext();
        var project = CreateProject();
        context.Projects.Add(project);
        context.HandoverDocuments.Add(new HandoverDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            DocumentNo = "BBBG-RETAIN",
            HandoverDate = new DateOnly(2026, 9, 20)
        });
        await context.SaveChangesAsync();

        var deleteProject = () => context.Database.ExecuteSqlAsync($"DELETE FROM [Projects] WHERE [Id] = {project.Id}");

        await deleteProject.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-20: Primary PM handover commits old end and new active membership atomically")]
    public async Task PrimaryProjectManagerHandover_CommitsAtomically()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var project = CreateProject();
        var oldManager = CreateUser(UserRoleCode.ProjectManager);
        var newManager = CreateUser(UserRoleCode.ProjectManager);
        var oldMembership = CreateActivePrimaryMembership(project.Id, oldManager.Id);
        context.AddRange(project, oldManager, newManager, oldMembership);
        await context.SaveChangesAsync();

        await using var transaction = await context.Database.BeginTransactionAsync();
        oldMembership.Status = ProjectMemberStatus.Ended;
        oldMembership.ValidTo = new DateOnly(2026, 9, 20);
        context.ProjectMembers.Add(CreateActivePrimaryMembership(project.Id, newManager.Id));
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        context.ChangeTracker.Clear();
        var memberships = await context.ProjectMembers.Where(member => member.ProjectId == project.Id).ToListAsync();
        memberships.Should().ContainSingle(member => member.Status == ProjectMemberStatus.Active && member.IsPrimary && member.UserId == newManager.Id);
        memberships.Should().ContainSingle(member => member.Status == ProjectMemberStatus.Ended && member.UserId == oldManager.Id);
    }

    private static ApplicationUser CreateUser(UserRoleCode roleCode) => new()
    {
        Id = Guid.NewGuid(),
        UserName = $"p220_{roleCode.ToDbCode().ToLowerInvariant()}_{Guid.NewGuid():N}",
        DisplayName = "P2-20 SQL fixture user",
        PasswordHash = "fixture-password-hash",
        RoleCode = roleCode,
        Status = UserStatus.Active,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static ProjectMember CreateActivePrimaryMembership(Guid projectId, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        UserId = userId,
        RoleCode = UserRoleCode.ProjectManager,
        IsPrimary = true,
        ValidFrom = new DateOnly(2026, 9, 20),
        Status = ProjectMemberStatus.Active
    };

    private static Project CreateProject() => new()
    {
        Id = Guid.NewGuid(),
        ProjectCode = $"PRJ-{Guid.NewGuid():N}",
        Name = "P2-20 SQL fixture project",
        Status = ProjectStatus.Active,
        CreatedAt = DateTimeOffset.UtcNow
    };
}

public sealed class P220ProjectMembershipModelTests
{
    [Fact(DisplayName = "P2-20: The project membership model rejects a non-PM primary member")]
    public void PrimaryMembershipModel_RequiresProjectManagerRole()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(ProjectMember));
        var constraints = entity!.GetCheckConstraints().Select(constraint => constraint.Sql).ToArray();

        constraints.Should().Contain("[IsPrimary] = 0 OR [RoleCode] = 'PM'");
    }

    private static RoadGuardDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RoadGuard_P220_ModelOnly;Integrated Security=true;TrustServerCertificate=true")
            .Options;
        return new RoadGuardDbContext(options);
    }
}

public sealed class P220ProjectMembershipMigrationLifecycleTests
{
    [Fact(DisplayName = "P2-20: Project schema migration downgrades to P2-04 and reapplies")]
    public async Task MigrationLifecycle_DowngradesToP204AndReapplies()
    {
        var fixture = new SqlServerTestFixture();
        await fixture.InitializeAsync();
        try
        {
            await using (var baseline = fixture.CreateDbContext())
            {
                await baseline.Database.EnsureDeletedAsync();
            }

            var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite())
                .Options;
            await using var context = new RoadGuardDbContext(options);
            await context.Database.MigrateAsync();

            (await ProjectTableCountAsync(context)).Should().Be(3);

            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync("20260919085118_AddImmutableFileStorageBoundary");
            (await ProjectTableCountAsync(context)).Should().Be(0);

            await context.Database.MigrateAsync();
            (await ProjectTableCountAsync(context)).Should().Be(3);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static Task<int> ProjectTableCountAsync(RoadGuardDbContext context) => context.Database.SqlQueryRaw<int>(
        """
        SELECT CAST(COUNT(*) AS int) AS [Value]
        FROM sys.tables
        WHERE [name] IN ('Projects', 'ProjectMembers', 'HandoverDocuments')
        """).SingleAsync();
}
