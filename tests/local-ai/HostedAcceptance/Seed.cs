using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Devices;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;
namespace HostedAcceptance;
public static partial class Entry {
 sealed record SeedData(Guid ProjectId,Guid RouteId,Guid SetId,Guid SegmentId,Guid ModelId,Guid DeviceId,Dictionary<string,string> Map);
 static async Task<SeedData> Seed(AuthenticationSqlServerFixture sql,ApplicationUser pm,ApplicationUser op){
  await using var db=sql.CreateDbContext(); var now=DateTimeOffset.UtcNow;
  var project=Project.Create(Guid.NewGuid(),"LOCAL-"+Guid.NewGuid().ToString("N"),"Local PM Crew AI",null,32648,null,null,now);
  var road=RoadSection.Create(Guid.NewGuid(),project.Id,"LOCAL");var line=new GeometryFactory(new PrecisionModel(),32648).CreateLineString([new(500000,1200000),new(500100,1200000)]);
  var route=RoadSectionVersion.Create(Guid.NewGuid(),road.Id,1,true,line,now,"Local fixture");var set=RoadSegmentSet.Create(Guid.NewGuid(),route.Id,"DRAFT");set.Define(JsonSerializer.Serialize(new SegmentDefinitionFact()),"local-fixture-hash");set.Publish(pm.Id,now);var segment=RoadSegment.Create(Guid.NewGuid(),set.Id,route.Id,1);segment.SetGeometry(0,100,0,line);
  var input=new GeometryDraftInputFact("MANUAL",32648,0,"Local fixture",[new(500000,1200000),new(500100,1200000)],null,null,null,[],10);
  var draft=RoadGeometryDraft.Create(project.Id,road.Id,"LOCAL","Local fixture",JsonSerializer.Serialize(input),"[]",null,pm.Id,now);
  db.AddRange(project,road,route,set,segment,draft,ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(),project.Id,pm.Id,new(2000,1,1)));
  foreach(var user in new[]{op})db.ProjectMembers.Add(new(){Id=Guid.NewGuid(),ProjectId=project.Id,UserId=user.Id,RoleCode=user.RoleCode,Status=ProjectMemberStatus.Active,ValidFrom=new(2000,1,1)});
  await db.SaveChangesAsync();draft.Submit(pm.Id,now);await db.SaveChangesAsync();draft.Confirm(road.Id);db.Add(RoadGeometryMetadata.Create(route.Id,draft.Id,JsonSerializer.Serialize(input),"local-fixture-hash",pm.Id,now:now));

 var model=AIModelVersion.Create(Guid.NewGuid(),"FT95",Guid.NewGuid().ToString("N"),"fixture/model",null,null,AIModelVersionStatus.Released,null,null);
 var map=Labels.Select((l,i)=>(l,c:l=="repair"?"REPAIR_PATCH":"FT95_"+i)).ToDictionary(x=>x.l,x=>x.c);
 var device=DroneDevice.Create(Guid.NewGuid(),"LOCAL-"+Guid.NewGuid().ToString("N"),DroneDeviceStatus.Active);
 db.AddRange(model,device); foreach(var code in map.Values)db.DefectTypes.Add(DefectType.Create(code,code));
 await db.SaveChangesAsync(); return new(project.Id,route.Id,set.Id,segment.Id,model.Id,device.Id,map);
 }
}
