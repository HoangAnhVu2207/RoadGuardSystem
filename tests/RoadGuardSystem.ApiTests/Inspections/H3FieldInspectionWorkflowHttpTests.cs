using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetTopologySuite.Geometries;
using System.Security.Cryptography;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Storage;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Inspections;
[Collection(AuthenticationApiFixture.Name)]
public sealed class H3FieldInspectionWorkflowHttpTests(AuthenticationSqlServerFixture sql)
{

    [Fact]
    public async Task Reporter_cannot_create_operational_FIELD_task()
    {
        var reporter=await sql.CreateUserAsync($"field-reporter-{Guid.NewGuid():N}","Current1!",UserRoleCode.Reporter);
        await using var factory=new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client=factory.CreateClient(new(){BaseAddress=new("https://localhost")});
        var login=await client.PostAsJsonAsync("/api/v1/auth/login",new{email=AuthenticationSqlServerFixture.EmailFor(reporter.UserName!),password="Current1!"});
        Assert.Equal(HttpStatusCode.OK,login.StatusCode);
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",(await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var request=new HttpRequestMessage(HttpMethod.Post,$"/api/v1/projects/{Guid.NewGuid()}/field-inspection-tasks")
        {Content=JsonContent.Create(new{defectId=Guid.NewGuid(),defectVersion="version",sourceKind="REPORTER",routeVersionId=Guid.NewGuid(),purpose="PRE_MEASUREMENT",requiredMeasurementType=1,measurementScope="{}",assignedToUserId=Guid.NewGuid(),dueAt=DateTimeOffset.UtcNow.AddDays(1)})};
        request.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Forbidden,(await client.SendAsync(request)).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_FIELD_wire_unknown_intake_task_upload_supplement_and_current_authority_are_scoped(bool genericDownload)
    {
        var pm=await sql.CreateUserAsync($"field-pm-{Guid.NewGuid():N}","Current1!",UserRoleCode.ProjectManager);
        var crew=await sql.CreateUserAsync($"field-crew-{Guid.NewGuid():N}","Current1!",UserRoleCode.RepairCrew);
        var other=await sql.CreateUserAsync($"field-other-{Guid.NewGuid():N}","Current1!",UserRoleCode.RepairCrew);
        var reporter=await sql.CreateUserAsync($"field-source-{Guid.NewGuid():N}","Current1!",UserRoleCode.Reporter);
        var scope=await Seed(pm.Id,crew.Id,other.Id,reporter.Id);
        var storage=new PhotoStorage();
        await using var factory=new AuthenticationWebApplicationFactory(sql.ConnectionString,configureTestServices:services=>{services.RemoveAll<IUploadObjectStorage>();services.AddSingleton<IUploadObjectStorage>(storage);});
        using var client=factory.CreateClient(new(){BaseAddress=new("https://localhost")});await Login(client,pm.UserName!);
        var root=$"/api/v1/projects/{scope.Project}/field-inspection-tasks";
        var body=new{defectId=scope.Defect,defectVersion=scope.Version,surveyId=(Guid?)null,sourceKind="REPORTER",routeVersionId=scope.Route,segmentSetId=scope.Set,purpose="PRE_MEASUREMENT",requiredMeasurementType=4,measurementScope="{}",instructions="measure only",assignedToUserId=crew.Id,dueAt=DateTimeOffset.UtcNow.AddDays(1)};
        var key=Guid.NewGuid().ToString();var response=await Post(client,root,body,key);Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        var task=await response.Content.ReadFromJsonAsync<JsonElement>();var id=task.GetProperty("id").GetGuid();var path=root+"/"+id;
        Assert.Equal("MEASURE_ONLY",task.GetProperty("mode").GetString());Assert.Equal(JsonValueKind.Null,task.GetProperty("surveyId").ValueKind);Assert.EndsWith(id.ToString(),response.Headers.Location!.ToString());Assert.Equal("\""+task.GetProperty("version").GetString()+"\"",response.Headers.ETag!.Tag);
        var replay=await Post(client,root,body,key);Assert.Equal(HttpStatusCode.OK,replay.StatusCode);Assert.Equal(task.GetRawText(),(await replay.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
        var malformed=new HttpRequestMessage(HttpMethod.Post,root){Content=JsonContent.Create(body)};malformed.Headers.Add("Idempotency-Key",new[]{"one","two"});Assert.Equal(HttpStatusCode.BadRequest,(await client.SendAsync(malformed)).StatusCode);
        await Login(client,other.UserName!);Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(path+"/geometry")).StatusCode);
        await Login(client,crew.UserName!);var geometry=await client.GetAsync(path+"/geometry");Assert.Equal(HttpStatusCode.OK,geometry.StatusCode);Assert.Equal(scope.Set,(await geometry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("segmentSetId").GetGuid());
        Assert.Equal(HttpStatusCode.Created,(await Post(client,path+"/accept",new{reason="accepted"},version:await Version(client,path))).StatusCode);
        var started=await Post(client,path+"/start",new{originId=Guid.NewGuid(),claimedAt=DateTimeOffset.UtcNow},version:await Version(client,path));Assert.Equal(HttpStatusCode.Created,started.StatusCode);var start=(await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var intakeBody=new{originId=Guid.NewGuid(),startOriginId=start,measurements=new[]{new{sampleId="area",type="Area",value=(decimal?)null,state="UNKNOWN",unknownReason="obscured",dimension="AREA",unit="m²",longitude=(double?)null,latitude=(double?)null,locationReason="GPS missing",instrument="gauge",method="area"}},evidence=new[]{new{captureOriginId=Guid.NewGuid(),fileId=(Guid?)null,purpose="MEASUREMENT",checksumSha256=PhotoStorage.Hash,mediaType="image/jpeg",capturedAt=DateTimeOffset.UtcNow}},locationProof=new{kind="UNKNOWN"},captureType="MEASUREMENT"};
        var bad=new{originId=Guid.NewGuid(),startOriginId=start,measurements=new object?[]{null},captureType="MEASUREMENT"};Assert.Equal(HttpStatusCode.BadRequest,(await Post(client,path+"/submissions",bad,version:await Version(client,path))).StatusCode);
        var intake=await Post(client,path+"/submissions",intakeBody,version:await Version(client,path));Assert.Equal(HttpStatusCode.Created,intake.StatusCode);var first=await intake.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal("INCOMPLETE",first.GetProperty("readiness").GetString());
        await Login(client,pm.UserName!);Assert.Equal(HttpStatusCode.Conflict,(await Post(client,path+"/reviews",new{submissionId=first.GetProperty("id").GetGuid(),decision="CONFIRM",reason="insufficient"},version:await Version(client,path))).StatusCode);
        var supplement=await Post(client,path+"/reviews",new{submissionId=first.GetProperty("id").GetGuid(),decision="SUPPLEMENT",reason="need actual capture"},version:await Version(client,path));Assert.Equal(HttpStatusCode.Created,supplement.StatusCode);Assert.Equal("BUSINESS_ACK_REQUIRED",(await supplement.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("receiptActivation").GetString());
        Guid receivingId;
        await using (var sourceDb = sql.CreateDbContext())
        {
            var receiving = await sourceDb.Set<BusinessReceivingRequest>().AsNoTracking().SingleAsync(r => r.ScopeId == id);
            receivingId = receiving.Id; Assert.Null(receiving.ClockId); Assert.Null(receiving.AcknowledgedAt);
        }
        var receivingPath = $"/api/v1/projects/{scope.Project}/receiving-requests/{receivingId}";
        var supervisor = await sql.CreateUserAsync($"activation-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        Guid reviewClockId; string reviewClockVersion; DateTimeOffset reviewOriginalDue;
        await using (var scopeDb = sql.CreateDbContext())
        {
            scopeDb.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = scope.Project, UserId = supervisor.Id,
                RoleCode = UserRoleCode.Supervisor, Status = ProjectMemberStatus.Active, ValidFrom = new(2000, 1, 1) });
            await scopeDb.SaveChangesAsync();
            var reviewClock = await scopeDb.Set<DeadlineClock>().AsNoTracking().SingleAsync(c => c.TargetId == id && c.Kind == DeadlineClockKind.ProjectManagerReview);
            reviewClockId = reviewClock.Id; reviewClockVersion = Convert.ToBase64String(reviewClock.RowVersion); reviewOriginalDue = reviewClock.OriginalDueAt;
        }
        var clockPath = $"/api/v1/projects/{scope.Project}/clocks/{reviewClockId}";
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(client, clockPath + "/extensions",
            new { newDueAt = reviewOriginalDue.AddHours(4), reason = "PM cannot extend own review" }, version: reviewClockVersion)).StatusCode);
        await Login(client, supervisor.UserName!);
        var extensionKey = Guid.NewGuid().ToString();
        var extended = await Post(client, clockPath + "/extensions", new { newDueAt = reviewOriginalDue.AddHours(4), reason = "Current Supervisor review extension" }, extensionKey, reviewClockVersion);
        Assert.Equal(HttpStatusCode.Created, extended.StatusCode);
        var extendedJson = await extended.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(reviewOriginalDue, extendedJson.GetProperty("originalDueAt").GetDateTimeOffset());
        Assert.Equal(reviewOriginalDue.AddHours(4), extendedJson.GetProperty("currentDueAt").GetDateTimeOffset());
        Assert.Equal("PENDING_OWNER_DECISION", extendedJson.GetProperty("numericalLimitPolicy").GetString());
        Assert.Equal(HttpStatusCode.OK, (await Post(client, clockPath + "/extensions",
            new { newDueAt = reviewOriginalDue.AddHours(4), reason = "Current Supervisor review extension" }, extensionKey, reviewClockVersion)).StatusCode);
        var appointmentVersion = extendedJson.GetProperty("version").GetString();
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(client, clockPath + "/appointments",
            new { assigneeId = crew.Id, reason = "Wrong duty role" }, version: appointmentVersion)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Post(client, clockPath + "/appointments",
            new { assigneeId = pm.Id, reason = "Current project PM review duty" }, version: appointmentVersion)).StatusCode);
        await Login(client, crew.UserName!);
        var waiting = await client.GetAsync(receivingPath); Assert.Equal(HttpStatusCode.OK, waiting.StatusCode);
        var waitingJson = await waiting.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Chưa xác nhận nhận", waitingJson.GetProperty("receivingState").GetString());
        Assert.Equal(JsonValueKind.Null, waitingJson.GetProperty("originalDueAt").ValueKind);
        var receivingVersion = waitingJson.GetProperty("version").GetString(); var receivingKey = Guid.NewGuid().ToString();
        var deviceTime = DateTimeOffset.UtcNow.AddDays(-1);
        var received = await Post(client, receivingPath + "/ack", new { claimedDeviceAt = deviceTime }, receivingKey, receivingVersion);
        Assert.Equal(HttpStatusCode.Created, received.StatusCode);
        var receivedJson = await received.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Đã nhận yêu cầu", receivedJson.GetProperty("receivingState").GetString());
        Assert.Equal(receivedJson.GetProperty("acknowledgedAt").GetDateTimeOffset().AddHours(48), receivedJson.GetProperty("originalDueAt").GetDateTimeOffset());
        Assert.Equal(deviceTime, receivedJson.GetProperty("claimedDeviceAt").GetDateTimeOffset());
        var receivedAgain = await Post(client, receivingPath + "/ack", new { claimedDeviceAt = deviceTime }, receivingKey, receivingVersion);
        Assert.Equal(HttpStatusCode.OK, receivedAgain.StatusCode);
        Assert.Equal(receivedJson.GetRawText(), (await receivedAgain.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
        await Login(client, other.UserName!);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(receivingPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(client, receivingPath + "/ack", new { claimedDeviceAt = deviceTime }, version: receivingVersion)).StatusCode);
        await Login(client,crew.UserName!);var uploadBody=new{projectId=scope.Project,targetId=id,purpose="MEASUREMENT",fileName="capture.jpg",mediaType="image/jpeg",sizeBytes=4,checksumSha256=PhotoStorage.Hash};
        var upload=await Post(client,"/api/v1/uploads",uploadBody);Assert.Equal(HttpStatusCode.Created,upload.StatusCode);var uploadJson=await upload.Content.ReadFromJsonAsync<JsonElement>();var uploadId=uploadJson.GetProperty("id").GetGuid();var file=uploadJson.GetProperty("fileId").GetGuid();
        Assert.Equal(HttpStatusCode.OK,(await Post(client,$"/api/v1/uploads/{uploadId}/part-urls",new{partNumbers=new[]{1}})).StatusCode);
        var uploadState=await client.GetAsync($"/api/v1/uploads/{uploadId}");Assert.Equal(HttpStatusCode.OK,uploadState.StatusCode);
        var complete=await Post(client,$"/api/v1/uploads/{uploadId}/complete",new{checksumSha256=PhotoStorage.Hash,parts=new[]{new{partNumber=1,eTag="part"}}},version:uploadState.Headers.ETag!.Tag.Trim('"'));Assert.True(complete.StatusCode==HttpStatusCode.Accepted,await complete.Content.ReadAsStringAsync());
        using(var verification=factory.Services.CreateScope()){await verification.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();}
        var follow=new{originId=Guid.NewGuid(),startOriginId=start,parentSubmissionId=first.GetProperty("id").GetGuid(),measurements=new[]{new{sampleId="area",type="Area",value=0m,state="KNOWN",dimension="AREA",unit="m²",locationReason="position checklist",instrument="gauge",method="area"}},evidence=new[]{new{captureOriginId=Guid.NewGuid(),fileId=file,purpose="MEASUREMENT",checksumSha256=PhotoStorage.Hash,mediaType="image/jpeg",capturedAt=DateTimeOffset.UtcNow}},locationProof=new{kind="POSITION_CHECKLIST",checklist="ROUTE_CHAINAGE_MARKINGS_CONFIRMED",observedRouteVersionId=scope.Route,observedChainageMeters=5},captureType="MEASUREMENT"};
        var submitted=await Post(client,path+"/submissions",follow,version:await Version(client,path));Assert.Equal(HttpStatusCode.Created,submitted.StatusCode);var second=await submitted.Content.ReadFromJsonAsync<JsonElement>();Assert.True(second.GetProperty("readiness").GetString()=="READY",second.GetRawText());Assert.Equal(first.GetProperty("originalReviewDueAt").GetDateTimeOffset(),second.GetProperty("originalReviewDueAt").GetDateTimeOffset());
        var metadata=await client.GetAsync(path+$"/evidence/{file}");Assert.Equal(HttpStatusCode.OK,metadata.StatusCode);var text=await metadata.Content.ReadAsStringAsync();Assert.DoesNotContain("objectKey",text,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("storageUri",text,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain(reporter.Id.ToString(),text,StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new byte[]{255,216,255,0},await client.GetByteArrayAsync(path+$"/evidence/{file}/content"));
        storage.FailOpen=true;var unavailable=await client.GetAsync(path+$"/evidence/{file}/content");Assert.Equal(HttpStatusCode.ServiceUnavailable,unavailable.StatusCode);var unavailableBody=await unavailable.Content.ReadAsStringAsync();Assert.Contains("storage_unavailable",unavailableBody);Assert.DoesNotContain("objectKey",unavailableBody,StringComparison.OrdinalIgnoreCase);storage.FailOpen=false;
        storage.AfterOpen=async()=>{await using var revoke=sql.CreateDbContext();await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={scope.Project} AND UserId={crew.Id}");};storage.Disposed=false;
        var racePath=genericDownload?$"/api/v1/files/{file}/content":path+$"/evidence/{file}/content";
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(racePath)).StatusCode);Assert.True(storage.Disposed);storage.AfterOpen=null;
        await Login(client,other.UserName!);Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(path+$"/evidence/{file}/content")).StatusCode);
        await Login(client,pm.UserName!);Assert.Equal(HttpStatusCode.Created,(await Post(client,path+"/reviews",new{submissionId=second.GetProperty("id").GetGuid(),decision="CONFIRM",reason="actual result"},version:await Version(client,path))).StatusCode);
        await using(var revoke=sql.CreateDbContext()){await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={scope.Project} AND UserId={pm.Id}");}
        Assert.Equal(HttpStatusCode.Forbidden,(await Post(client,root,body,key)).StatusCode);
    }
    private static async Task Login(HttpClient client,string user){var r=await client.PostAsJsonAsync("/api/v1/auth/login",new{email=AuthenticationSqlServerFixture.EmailFor(user),password="Current1!"});Assert.Equal(HttpStatusCode.OK,r.StatusCode);client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",(await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());}
    private static async Task<string> Version(HttpClient client,string path){var r=await client.GetAsync(path);Assert.Equal(HttpStatusCode.OK,r.StatusCode);return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString()!;}
    private static Task<HttpResponseMessage> Post(HttpClient client,string path,object body,string? key=null,string? version=null){var r=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(body)};r.Headers.Add("Idempotency-Key",key??Guid.NewGuid().ToString());if(version is not null)r.Headers.Add("If-Match","\""+version+"\"");return client.SendAsync(r);}
    private async Task<(Guid Project,Guid Route,Guid Set,Guid Defect,string Version)> Seed(Guid pm,Guid crew,Guid other,Guid reporter)
    {
        await using var db=sql.CreateDbContext();var now=DateTimeOffset.UtcNow;var project=Project.Create(Guid.NewGuid(),Guid.NewGuid().ToString(),"FIELD HTTP",null,null,null,null,now);var road=RoadSection.Create(Guid.NewGuid(),project.Id,"fixture");var line=new GeometryFactory(new PrecisionModel(),32648).CreateLineString([new(0,0),new(20,0)]);var route=RoadSectionVersion.Create(Guid.NewGuid(),road.Id,1,true,line,now,"fixture");var set=RoadSegmentSet.Create(Guid.NewGuid(),route.Id);var segment=RoadSegment.Create(Guid.NewGuid(),set.Id,route.Id,1);segment.SetGeometry(0,20,0,line);var type=DefectType.Create("F"+Guid.NewGuid().ToString("N"),"FIELD");
        db.AddRange(project,road,route,set,segment,type,ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(),project.Id,pm,new(2000,1,1)),new ProjectMember{Id=Guid.NewGuid(),ProjectId=project.Id,UserId=crew,RoleCode=UserRoleCode.RepairCrew,Status=ProjectMemberStatus.Active,ValidFrom=new(2000,1,1)},new ProjectMember{Id=Guid.NewGuid(),ProjectId=project.Id,UserId=other,RoleCode=UserRoleCode.RepairCrew,Status=ProjectMemberStatus.Active,ValidFrom=new(2000,1,1)});await db.SaveChangesAsync();
        var file=StoredFile.Create(Guid.NewGuid(),"reporter/private-"+Guid.NewGuid().ToString("N"),"source.jpg","image/jpeg",4,new string('b',64),reporter,now,null);db.AddRange(file,FileScope.CreatePrivate(Guid.NewGuid(),file.Id,reporter,now));await db.SaveChangesAsync();var report=Report.Create(Guid.NewGuid(),reporter,"genuine noSurvey source",now,[VerifiedEvidenceReference.Create(Guid.NewGuid(),file.Id,"fixture-version",reporter)]);var incident=IncidentCase.CreateUnassigned(Guid.NewGuid(),report.Id,now);incident.Triage(project.Id,CaseVerificationMethod.ExistingEvidence,"retained source",now);db.AddRange(report,incident);db.Set<HuyCaseReportLink>().Add(new(){Id=Guid.NewGuid(),CaseId=incident.Id,ReportId=report.Id,StartedAt=now});await db.SaveChangesAsync();
        var facts=CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report,report.Id,"fixture-source"),project.Id,"fixture-geometry");var accepted=await new CandidateDecisionRepository(db).SaveAcceptedAsync(pm,facts,CandidateDecisionKind.KeepNew,CandidateClassification.Create(route.Id,type.Code,null,DefectSeverity.Low,null),null,null,null,"PM keep-new",null,default);
        var version=await db.Defects.Where(x=>x.Id==accepted.DefectId).Select(x=>EF.Property<byte[]>(x,"RowVersion")).SingleAsync();return(project.Id,route.Id,set.Id,accepted.DefectId!.Value,Convert.ToBase64String(version));
    }
    private sealed class PhotoStorage:IUploadObjectStorage
    {
        public static string Hash=>Convert.ToHexString(SHA256.HashData(new byte[]{255,216,255,0})).ToLowerInvariant();
        public Task<string> InitiateAsync(string key,string type,CancellationToken cancellationToken=default)=>Task.FromResult("multipart");
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string key,string id,IReadOnlyList<int> parts,DateTimeOffset expires,CancellationToken cancellationToken=default)=>Task.FromResult<IReadOnlyList<PresignedUploadPart>>(parts.Select(x=>new PresignedUploadPart(x,"https://storage.invalid/part",expires)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string key,string id,IReadOnlyList<CompletedStoragePart> parts,CancellationToken cancellationToken=default)=>Task.FromResult(new UploadObjectVerification(4,Hash,"image/jpeg"));
        public Func<Task>? AfterOpen;public bool Disposed;public bool FailOpen;
        public async Task<Stream> OpenReadAsync(string key,CancellationToken cancellationToken=default){if(FailOpen)throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable,"Storage unavailable.");var stream=new TrackingStream(this);if(AfterOpen is not null)await AfterOpen();return stream;}
        private sealed class TrackingStream(PhotoStorage owner):MemoryStream(new byte[]{255,216,255,0})
        {protected override void Dispose(bool disposing){owner.Disposed=true;base.Dispose(disposing);}}
    }
}
