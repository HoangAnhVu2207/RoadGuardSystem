using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Implementations.Processing;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Processing.Worker;
namespace PmCrewHost;
public static class Entry
{
 const string Root="D:/RoadGuard-AI-LocalTest/pm-crew";
 const string Video="C:/Users/HoangAnhVu/Downloads/DJI_0060_01m00s-02m00s.MP4";
 const string VideoHash="8f8b0c2dc1bf5fb11579323c64281a8897cb8268b26667687ecffb4f25211932";
 const string ModelHash="bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0";
 static readonly string[] Labels=["alligator crack","longitudinal crack","oblique crack","pothole","repair","transverse crack"];
 public static async Task Main(string[] args)
 {
  if(Environment.GetEnvironmentVariable("ROADGUARD_RUN_PM_CREW")!="1") throw new InvalidOperationException("Explicit local opt-in ROADGUARD_RUN_PM_CREW=1 required.");
  var repo=Path.GetFullPath(args.Single()); Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_ROADGUARDSYSTEM_EAPI",Path.Combine(repo,"RoadGuardSystem.API")); Directory.SetCurrentDirectory(AppContext.BaseDirectory);
  Directory.CreateDirectory(Root); File.Delete(Path.Combine(Root,"stop.request"));
  if(new DriveInfo("D:/").AvailableFreeSpace<32L*1024*1024*1024) throw new InvalidOperationException("D: reserve insufficient.");
  if(new DirectoryInfo("D:/RoadGuard-AI-LocalTest").EnumerateFiles("*",SearchOption.AllDirectories).Sum(f=>f.Length)>38L*1024*1024*1024) throw new InvalidOperationException("Local test budget exceeded.");
  await using(var source=File.OpenRead(Video)) if(Convert.ToHexString(await SHA256.HashDataAsync(source)).ToLowerInvariant()!=VideoHash) throw new InvalidOperationException("Authorized sample hash mismatch.");
  var secret=Convert.ToHexString(RandomNumberGenerator.GetBytes(24)); var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
  var tokenFile=Path.Combine(Root,"worker.token"); await File.WriteAllTextAsync(tokenFile,token);
  await using var sql=new AuthenticationSqlServerFixture();
  await using var minio=new ContainerBuilder("rg-low-resource-20261030-minio:latest").WithEnvironment("MINIO_ROOT_USER","pmcrewfixture").WithEnvironment("MINIO_ROOT_PASSWORD",secret).WithCommand("server","/data").WithPortBinding(9000,true).WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r=>r.ForPort(9000).ForPath("/minio/health/ready"))).Build();
  await minio.StartAsync(); await sql.InitializeAsync();
  var endpoint=$"http://{minio.Hostname}:{minio.GetMappedPublicPort(9000)}";
  var storage=new MinioStorageOptions{Endpoint=endpoint,BucketName="pm-crew",AccessKey="pmcrewfixture",SecretKey=secret};
  using var s3=new AmazonS3Client(storage.AccessKey,secret,new AmazonS3Config{ServiceURL=endpoint,ForcePathStyle=true,AuthenticationRegion="us-east-1"}); await s3.PutBucketAsync(storage.BucketName);
  var password="Local1!"+Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
  var pm=await sql.CreateUserAsync("pm-"+Guid.NewGuid().ToString("N"),password,UserRoleCode.ProjectManager);
  var crew=await sql.CreateUserAsync("crew-"+Guid.NewGuid().ToString("N"),password,UserRoleCode.RepairCrew);
  var op=await sql.CreateUserAsync("operator-"+Guid.NewGuid().ToString("N"),password,UserRoleCode.DroneOperator);
   await using var factory=new AuthenticationWebApplicationFactory(sql.ConnectionString,accessTokenLifetimeMinutes:60,configureTestServices:services=>{services.Configure<MinioStorageOptions>(o=>{o.Endpoint=endpoint;o.BucketName=storage.BucketName;o.AccessKey=storage.AccessKey;o.SecretKey=secret;}); services.AddHostedService<RoadGuardSystem.API.Workers.UploadVerificationWorker>();},allowExternalStorage:true);
   using var backend=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
   using(var authenticationProbe=await backend.PostAsJsonAsync("/api/v1/auth/login",new{email=pm.Email,password})){
    if(!authenticationProbe.IsSuccessStatusCode) throw new InvalidOperationException("Real PM login preflight failed.");
    var authenticationJson=await authenticationProbe.Content.ReadFromJsonAsync<JsonElement>();
    using var me=new HttpRequestMessage(HttpMethod.Get,"/api/v1/me"); me.Headers.Authorization=new("Bearer",authenticationJson.GetProperty("accessToken").GetString());
    using var meResponse=await backend.SendAsync(me); if(!meResponse.IsSuccessStatusCode)throw new InvalidOperationException("Real bearer validation preflight failed: "+(int)meResponse.StatusCode);
   }
  var seed=await Seed(sql,pm,crew,op);
  await using(var input=File.OpenRead(Video)) await s3.PutObjectAsync(new PutObjectRequest{BucketName=storage.BucketName,Key=seed.File.StorageUri,InputStream=input,ContentType="video/mp4"});
  var tools="C:/Users/HoangAnhVu/AppData/Local/Temp/roadguard-media-tools/ffmpeg-9.0.2-essentials_build/bin";
  var psi=new ProcessStartInfo("D:/RoadGuard-AI-LocalTest/venv/Scripts/python.exe"){WorkingDirectory=Path.Combine(repo,"AI/roadguard-ai-engine"),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
  foreach(var arg in new[]{"-m","uvicorn","src.api.be_worker_runtime:create_app_from_env","--factory","--host","127.0.0.1","--port","8997","--no-access-log"}) psi.ArgumentList.Add(arg);
  psi.Environment["ROADGUARD_WORKER_DATA_ROOT"]=Path.Combine(Root,"worker"); psi.Environment["ROADGUARD_WORKER_TOKEN_FILE"]=tokenFile; psi.Environment["ROADGUARD_WORKER_CHECKPOINT"]=Path.Combine(repo,"AI/roadguard-ai-engine/weights/trained/RoadGuard_YOLO11_UAVPDD2023_FT95_v1.pt"); psi.Environment["ROADGUARD_WORKER_FFMPEG"]=Path.Combine(tools,"ffmpeg.exe"); psi.Environment["ROADGUARD_WORKER_FFPROBE"]=Path.Combine(tools,"ffprobe.exe"); psi.Environment["TEMP"]=Root; psi.Environment["TMP"]=Root; psi.Environment["YOLO_CONFIG_DIR"]=Path.Combine(Root,"ultralytics");
  using var worker=Process.Start(psi)??throw new InvalidOperationException("Worker start failed.");
  var stdout=worker.StandardOutput.ReadToEndAsync(); var stderr=worker.StandardError.ReadToEndAsync();
  try{
   using var workerHttp=AiWorkerClient.CreateHttpClient(); workerHttp.DefaultRequestHeaders.Authorization=new("Bearer",token); var origin=new Uri("http://127.0.0.1:8997/");
   var ready=false; for(var i=0;i<120;i++){if(worker.HasExited)throw new InvalidOperationException("Private worker exited.");try{using var health=await workerHttp.GetAsync(new Uri(origin,"internal/v1/health"));if(health.IsSuccessStatusCode){ready=true;break;}}catch(HttpRequestException){}await Task.Delay(500);} if(!ready)throw new InvalidOperationException("Worker readiness timeout.");
   var client=new AiWorkerClient(workerHttp,origin,token);
   var options=new AiWorkerRuntimeOptions{Enabled=true,ReleasedModelVersionId=seed.ModelId,CheckpointSha256=ModelHash,ClassMap=seed.Map,TempRoot=Path.Combine(Root,"be-spool"),FfprobePath=Path.Combine(tools,"ffprobe.exe"),MaxFrames=60,PollInterval=TimeSpan.FromSeconds(1)};
   var artifacts=new MinioAnh02ArtifactStore(Options.Create(storage)); var uploads=new MinioUploadObjectStorage(Options.Create(storage));
   var timer=Stopwatch.StartNew(); Guid workId=default;
   while(timer.Elapsed<TimeSpan.FromMinutes(12)){
    if(File.Exists(Path.Combine(Root,"stop.request")))throw new OperationCanceledException("Local fixture stop requested during inference.");
    await using var db=sql.CreateDbContext(); using var coordinator=new AiVideoCoordinator(new AiVideoWorkRepository(db),uploads,artifacts,client,new AiWorkerMediaProbe(options.FfprobePath),options,geometry:new GeometryWorkflowPersistenceService(db,new IdempotencyOperationService(db)));
    await coordinator.TickAsync("persistent-pm-crew"); var work=await db.AiVideoWorkItems.AsNoTracking().SingleOrDefaultAsync();
    if(work is not null){workId=work.Id;if(work.State==AiVideoWorkState.Ingested)break;if(work.State is AiVideoWorkState.TerminalFailed or AiVideoWorkState.RetryableFailed)throw new InvalidOperationException("Real inference failed: "+work.ErrorCode);}
    await Task.Delay(1000);
   }
   await using var verify=sql.CreateDbContext(); var accepted=await verify.AiVideoWorkItems.AsNoTracking().SingleAsync(); if(accepted.State!=AiVideoWorkState.Ingested)throw new InvalidOperationException("Actual BE acceptance timeout.");
   using(var diagnosticsScope=factory.Services.CreateScope()){
    var reader=diagnosticsScope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Integration.IAiCandidateFactsReader>();
    var first=await verify.AIDetections.AsNoTracking().Select(d=>d.Id).FirstAsync();
    var readiness=await reader.ResolveAsync(pm.Id,UserRoleCode.ProjectManager,seed.ProjectId,first);
    Console.WriteLine("CANDIDATE_READINESS "+readiness.Status);
   }
   var accounts=new Dictionary<string,object>{{"PM",new{email=pm.Email,password}},{"CREW",new{email=crew.Email,password}}};
   var detectionIds=await verify.AIDetections.AsNoTracking().Select(d=>d.Id).ToArrayAsync(); var fixture=new{storageOrigin=endpoint,detectionIds,classMap=seed.Map,backendUrl="http://127.0.0.1:5087",projectId=seed.ProjectId,surveyId=seed.SurveyId,datasetId=seed.DatasetId,routeVersionId=seed.RouteId,segmentSetId=seed.SetId,segmentId=seed.SegmentId,chainageRange=new{min=0,max=100},crewUserId=crew.Id,workId,processingJobId=accepted.ProcessingJobId,classificationCodes=seed.Map,defectDetailRoute=$"/api/v1/projects/{seed.ProjectId}/defects/{{defectId}}",geometryEvidence="CURRENT_VERIFIED real SQL geometry repository; synthetic straight-line local fixture, not surveyed geometry",accounts};
   await File.WriteAllTextAsync(Path.Combine(Root,"fixture.json"),JsonSerializer.Serialize(fixture,new JsonSerializerOptions{WriteIndented=true}));
   await File.WriteAllTextAsync(Path.Combine(Root,"resources.json"),JsonSerializer.Serialize(new{hostPid=Environment.ProcessId,workerPid=worker.Id,sqlContainerId=sql.GetOwnedSeedDatabaseIdentity().ContainerId,minioContainerId=minio.Id,ownedRoot=Root}));
   var builder=WebApplication.CreateBuilder(new WebApplicationOptions{Args=[],ContentRootPath=repo}); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:5087"); var app=builder.Build();
   app.Run(async context=>{
    var peer=context.Connection.RemoteIpAddress;
    if(peer is null||!IPAddress.IsLoopback(peer)||context.Request.Headers.ContainsKey("Origin")){context.Response.StatusCode=403;return;}
    var target=context.Request.Path+context.Request.QueryString;
    if(!context.Request.Path.StartsWithSegments("/api")&&context.Request.Path!="/health"){context.Response.StatusCode=404;return;}
    using var request=new HttpRequestMessage(new HttpMethod(context.Request.Method),target);
    if(context.Request.ContentLength>0){request.Content=new StreamContent(context.Request.Body);if(context.Request.ContentType is not null)request.Content.Headers.TryAddWithoutValidation("Content-Type",context.Request.ContentType);}
    foreach(var name in new[]{"Authorization","If-Match","Idempotency-Key","Accept","X-Correlation-ID"})if(context.Request.Headers.TryGetValue(name,out var value))request.Headers.TryAddWithoutValidation(name,value.ToArray());
    using var response=await backend.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,context.RequestAborted);context.Response.StatusCode=(int)response.StatusCode;
    foreach(var header in response.Headers.Concat(response.Content.Headers))if(!new[]{"Transfer-Encoding","Connection"}.Contains(header.Key,StringComparer.OrdinalIgnoreCase))context.Response.Headers[header.Key]=header.Value.ToArray();
    await response.Content.CopyToAsync(context.Response.Body,context.RequestAborted);
   });
   _ = Task.Run(async()=>{while(!app.Lifetime.ApplicationStopping.IsCancellationRequested){if(File.Exists(Path.Combine(Root,"stop.request"))){app.Lifetime.StopApplication();break;}await Task.Delay(1000);}}); Console.WriteLine("PM_CREW_READY http://127.0.0.1:5087 (metadata/credentials kept in protected D: runtime file)"); await app.RunAsync();
  }finally{if(!worker.HasExited){worker.Kill(entireProcessTree:true);await worker.WaitForExitAsync();}await File.WriteAllTextAsync(Path.Combine(Root,"worker.stdout.log"),await stdout);await File.WriteAllTextAsync(Path.Combine(Root,"worker.stderr.log"),await stderr);File.Delete(tokenFile);}
 }
 sealed record SeedData(Guid ProjectId,Guid SurveyId,Guid DatasetId,Guid RouteId,Guid SetId,Guid SegmentId,StoredFile File,Guid ModelId,Dictionary<string,string> Map);
 static async Task<SeedData> Seed(AuthenticationSqlServerFixture sql,ApplicationUser pm,ApplicationUser crew,ApplicationUser op){
  await using var db=sql.CreateDbContext(); var now=DateTimeOffset.UtcNow;
  var project=Project.Create(Guid.NewGuid(),"LOCAL-"+Guid.NewGuid().ToString("N"),"Local PM Crew AI",null,32648,null,null,now);
  var road=RoadSection.Create(Guid.NewGuid(),project.Id,"LOCAL");var line=new GeometryFactory(new PrecisionModel(),32648).CreateLineString([new(500000,1200000),new(500100,1200000)]);
  var route=RoadSectionVersion.Create(Guid.NewGuid(),road.Id,1,true,line,now,"Local fixture");var set=RoadSegmentSet.Create(Guid.NewGuid(),route.Id,"DRAFT");set.Define(JsonSerializer.Serialize(new SegmentDefinitionFact()),"local-fixture-hash");set.Publish(pm.Id,now);var segment=RoadSegment.Create(Guid.NewGuid(),set.Id,route.Id,1);segment.SetGeometry(0,100,0,line);
  var input=new GeometryDraftInputFact("MANUAL",32648,0,"Local fixture",[new(500000,1200000),new(500100,1200000)],null,null,null,[],10);
  var draft=RoadGeometryDraft.Create(project.Id,road.Id,"LOCAL","Local fixture",JsonSerializer.Serialize(input),"[]",null,pm.Id,now);
  db.AddRange(project,road,route,set,segment,draft,ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(),project.Id,pm.Id,new(2000,1,1)));
  foreach(var user in new[]{crew,op})db.ProjectMembers.Add(new(){Id=Guid.NewGuid(),ProjectId=project.Id,UserId=user.Id,RoleCode=user.RoleCode,Status=ProjectMemberStatus.Active,ValidFrom=new(2000,1,1)});
  await db.SaveChangesAsync();draft.Submit(pm.Id,now);await db.SaveChangesAsync();draft.Confirm(road.Id);db.Add(RoadGeometryMetadata.Create(route.Id,draft.Id,JsonSerializer.Serialize(input),"local-fixture-hash",pm.Id,now:now));
  var request=SurveyRequest.Create(Guid.NewGuid(),project.Id,road.Id,null,pm.Id,SurveyType.Periodic,SurveyRequestStatus.Accepted,now,roadSectionVersionId:route.Id);
  var survey=Survey.Create(Guid.NewGuid(),request.Id,project.Id,route.Id,SurveyType.Periodic,SurveyStatus.InProgress,false,null,null);
  var file=StoredFile.Create(Guid.NewGuid(),"fixture/"+Guid.NewGuid().ToString("N"),"video.mp4","video/mp4",new FileInfo(Video).Length,VideoHash,op.Id,now,null);
  var dataset=SurveyDataVersion.CreateSubmitted(Guid.NewGuid(),survey.Id,1,now,now,Guid.NewGuid(),JsonSerializer.Serialize(new[]{new{fileId=file.Id,checksumSha256=file.Checksum,sizeBytes=file.SizeBytes,purpose="SURVEY_VIDEO"}}),JsonSerializer.Serialize(new[]{new{routeVersionId=route.Id,segmentSetId=set.Id,segmentIds=new[]{segment.Id}}}));
  dataset.SetSubmissionProvenance(op.Id,"[]"); var model=AIModelVersion.Create(Guid.NewGuid(),"FT95",Guid.NewGuid().ToString("N"),"fixture/model",null,null,AIModelVersionStatus.Released,null,null);var map=Labels.Select((l,i)=>(l,c:l=="repair"?"REPAIR_PATCH":"FT95_"+i)).ToDictionary(x=>x.l,x=>x.c);
  db.AddRange(request,survey,file,dataset,model,FileScope.Create(Guid.NewGuid(),file.Id,project.Id,request.Id,op.Id,"SURVEY_VIDEO",now),SurveyRequestScope.Create(Guid.NewGuid(),request.Id,route.Id,set.Id,JsonSerializer.Serialize(new[]{segment.Id}),"SURFACE"),SurveyAssignment.Create(Guid.NewGuid(),request.Id,op.Id,pm.Id,now,now,null,null,null,null));foreach(var code in map.Values)db.DefectTypes.Add(DefectType.Create(code,code));
  var upload=UploadSession.Create(Guid.NewGuid(),file.Id,op.Id,file.StorageUri,"SURVEY_VIDEO","video/mp4",file.SizeBytes,file.Checksum,checked((int)file.SizeBytes),now.AddHours(1));db.Add(upload);await db.SaveChangesAsync();upload.StartUploading("fixture",now);await db.SaveChangesAsync();upload.StartVerification(Convert.ToBase64String(upload.RowVersion),now);await db.SaveChangesAsync();upload.MarkVerified();await db.SaveChangesAsync();db.OutboxMessages.Add(OutboxMessage.Create(Guid.NewGuid(),"ai.real.dataset_submitted.v1",now,dataset.Id,"{}"));await db.SaveChangesAsync();
  return new(project.Id,survey.Id,dataset.Id,route.Id,set.Id,segment.Id,file,model.Id,map);
 }
}










\n