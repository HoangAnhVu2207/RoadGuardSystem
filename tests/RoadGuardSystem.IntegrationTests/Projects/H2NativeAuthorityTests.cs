using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.SqlClient;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
namespace RoadGuardSystem.IntegrationTests.Projects;

public sealed class H2NativeAuthorityTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture=new(createSpatialProbeSchema:false);
    public async Task InitializeAsync(){await fixture.InitializeAsync();await using var db=Db();await db.Database.MigrateAsync();}
    public Task DisposeAsync()=>fixture.DisposeAsync();
    private RoadGuardDbContext Db(DbCommandInterceptor? probe=null)
    {
        var options=new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString,s=>s.UseNetTopologySuite());
        if(probe is not null) options.AddInterceptors(probe);
        return new(options.Options);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task Current_membership_is_locked_through_read_or_fresh_write_and_checked_on_replay(bool write)
    {
        var actor=Guid.NewGuid();var project=Guid.NewGuid();
        await using(var seed=Db())
        {
            await seed.Database.ExecuteSqlRawAsync("IF NOT EXISTS(SELECT 1 FROM Roles WHERE Code='PM') INSERT Roles(Code,Name,NormalizedName,IsActive) VALUES('PM','ProjectManager','PM',1)");
            seed.Projects.Add(Project.Create(project,project.ToString(),"authority",null,null,null,null,DateTimeOffset.UtcNow));
            seed.Users.Add(new ApplicationUser{Id=actor,UserName=actor.ToString(),NormalizedUserName=actor.ToString().ToUpperInvariant(),DisplayName="fixture",RoleCode=UserRoleCode.ProjectManager,Status=UserStatus.Active,CreatedAt=DateTimeOffset.UtcNow,PasswordHash="fixture"});
            seed.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(),project,actor,new(2000,1,1)));await seed.SaveChangesAsync();
        }
        var probe=new ProfileProbe(async token=>
        {
            await using var revoke=Db();await revoke.Database.OpenConnectionAsync(token);await revoke.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 500",token);
            var denied=await Assert.ThrowsAsync<SqlException>(()=>revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={project} AND UserId={actor}",token));
            Assert.Equal(1222,denied.Number);
        });
        await using var db=Db(probe);var repo=new GeometryWorkflowPersistenceService(db,new IdempotencyOperationService(db));
        var profile=new CrsProfileInput("sample",0,"sample datum","sample projection","EN",1,"synthetic fixture",new string('a',64));
        var command=new GeometryWorkflowCommand(actor,project,write?"profile-create":"profile-list",null,null,null,null,write?profile:null,write?Guid.NewGuid().ToString():null,null,UserRoleCode.ProjectManager);
        Assert.Equal(write?201:200,(await repo.ExecuteAsync(command,GeometryEngine.Preview,GeometryEngine.Segments,default)).Status);Assert.Equal(1,probe.Calls);
        await using(var revoke=Db()) await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={project} AND UserId={actor}");
        Assert.Equal(403,(await repo.ExecuteAsync(command,GeometryEngine.Preview,GeometryEngine.Segments,default)).Status);
    }
    private sealed class ProfileProbe(Func<CancellationToken,Task> callback):DbCommandInterceptor
    {
        public int Calls{get;private set;}
        public override async ValueTask<System.Data.Common.DbDataReader> ReaderExecutedAsync(System.Data.Common.DbCommand command,CommandExecutedEventData eventData,System.Data.Common.DbDataReader result,CancellationToken cancellationToken=default)
        {
            if(Calls==0 && command.CommandText.Contains("FROM [CrsProfileRevisions]",StringComparison.Ordinal)){Calls++;await callback(cancellationToken);}
            return result;
        }
    }
}
