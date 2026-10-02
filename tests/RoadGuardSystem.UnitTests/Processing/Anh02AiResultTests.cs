using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Processing.Anh02;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
namespace RoadGuardSystem.UnitTests.Processing;
public sealed class Anh02AiResultTests
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    private static AiDetectionResultV1 Detection(Guid id)=>new(id,Guid.NewGuid(),Guid.NewGuid(),new string('a',64),500,"CRACK",.8m,[.375m,.333333333m,.25m,.333333333m],null,"UNKNOWN");
    private static AiResultV1 Result()=>new("anh02.ai.v1",Guid.NewGuid(),"VIDEO_ANALYSIS","MOCK",Guid.NewGuid(),Guid.NewGuid(),new string('b',64),Guid.NewGuid(),"synthetic-road-v1","not part of canonical bytes",DateTimeOffset.Parse("2026-10-02T22:00:00+07:00"),[Detection(Guid.NewGuid()),Detection(Guid.NewGuid())],[]);
    [Fact]
    public void Canonical_bytes_sort_sets_preserve_box_order_utc_and_original_hash()
    {
        var input=Result();var bytes=AiResultCanonicalizer.Encode(input);
        Assert.Equal(bytes,AiResultCanonicalizer.Encode(input with {Detections=input.Detections.Reverse().ToArray(),ResultHash="another",CompletedAt=input.CompletedAt.ToUniversalTime()}));
        using var json=JsonDocument.Parse(bytes);Assert.Equal("2026-10-02T15:00:00.0000000Z",json.RootElement.GetProperty("completedAt").GetString());
        Assert.Equal(.375m,json.RootElement.GetProperty("detections")[0].GetProperty("bbox")[0].GetDecimal());
        Assert.Equal(JsonValueKind.Null,json.RootElement.GetProperty("detections")[0].GetProperty("geometry").ValueKind);
        Assert.NotEqual(AiManifestCanonicalizer.Hash(bytes),AiManifestCanonicalizer.Hash(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes)+" ")));
    }
    [Fact]
    public void Nonhex_manifest_hash_is_rejected()=>Assert.Throws<ArgumentException>(()=>AiResultCanonicalizer.Encode(Result() with{ManifestHash=new string('z',64)}));
    [Fact]
    public void Null_detection_is_contract_error_not_null_dereference()=>Assert.Throws<ArgumentException>(()=>AiResultCanonicalizer.Encode(Result() with{Detections=[null!]}));
    [Fact]
    public void Duplicate_matching_rank_and_candidate_are_ambiguous_and_rejected()
    {
        var d=Guid.NewGuid();var c=Guid.NewGuid();var match=new AiMatchResultV1(d,c,"version",1,null,["MOCK"]);
        Assert.Throws<ArgumentException>(()=>AiResultCanonicalizer.Encode(Result() with{Stage="DUPLICATE_MATCHING",Detections=[],Matches=[match,match]}));
    }
    [Fact]
    public void Box_beyond_normalized_image_is_rejected()
    {var input=Result();Assert.Throws<ArgumentException>(()=>AiResultCanonicalizer.Encode(input with{Detections=[input.Detections[0] with{Bbox=[.9m,0,.2m,.2m]}]}));}
    [Theory]
    [InlineData("Production",true)]
    [InlineData("Development",false)]
    [InlineData("Staging",true)]
    public async Task Mock_requires_explicit_flag_and_development_or_test_environment(string environment,bool enabled)
    {
        var service=new Anh02AiService(null!,null!,null!,null!,null!,[],Options.Create(new Anh02AiOptions{MockEnabled=enabled}),new EnvironmentFixture(environment),TimeProvider.System);
        var error=await Assert.ThrowsAsync<AiRequestException>(()=>service.GetAsync(Guid.NewGuid(),UserRoleCode.ProjectManager,Guid.NewGuid(),Guid.NewGuid(),default));Assert.Equal(404,error.Status);
    }
    [Theory]
    [InlineData("analysis")]
    [InlineData("matching")]
    public void Checked_in_samples_match_Csharp_canonical_bytes(string stage)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root is not null && !Directory.Exists(Path.Combine(root.FullName,"contracts","ai","fixtures","anh02")))root=root.Parent;
        Assert.NotNull(root);var path=Path.Combine(root.FullName,"contracts","ai","fixtures","anh02");
        var manifestBytes=File.ReadAllBytes(Path.Combine(path,stage+"-manifest.sample.json"));using var doc=JsonDocument.Parse(manifestBytes);var m=doc.RootElement;
        Guid Id(string n)=>m.GetProperty(n).GetGuid();string Text(string n)=>m.GetProperty(n).GetString()!;
        var matching=stage=="matching";
        var draft=new AiManifestDraft(Id("runId"),Text("stage"),Id("projectId"),Id("jobId"),Id("attemptId"),Id("datasetVersionId"),Text("datasetManifestHash"),
            Id("routeVersionId"),Id("segmentSetId"),Text("geometryVersion"),m.GetProperty("segmentIds").EnumerateArray().Select(x=>x.GetGuid()).ToArray(),Text("targetBand"),
            Id("modelVersionId"),Text("preprocessingVersion"),Text("configVersion"),Text("fixtureVersion"),Id("createdBy"),m.GetProperty("createdAt").GetDateTimeOffset(),
            m.GetProperty("sourceFiles").EnumerateArray().Select(f=>new AiManifestFileDraft(f.GetProperty("fileId").GetGuid(),f.GetProperty("fileVersion").GetString()!,f.GetProperty("sha256").GetString()!,f.GetProperty("sizeBytes").GetInt64(),f.GetProperty("mediaType").GetString()!,f.GetProperty("purpose").GetString()!)).ToArray(),[],Text("telemetryStatus"),
            matching?Id("analysisResultId"):null,matching?Id("candidateSnapshotId"):null,matching?Text("candidateSnapshotHash"):null,
            matching?m.GetProperty("items").EnumerateArray().Select(i=>new AiMatchingItemDraft(i.GetProperty("defectId").GetGuid(),i.GetProperty("version").GetString()!,i.GetProperty("segmentId").GetGuid(),i.GetProperty("routeVersionId").GetGuid())).ToArray():null);
        Assert.Equal(manifestBytes,AiManifestCanonicalizer.Encode(draft));
        var resultBytes=File.ReadAllBytes(Path.Combine(path,stage+"-result.sample.json"));var result=JsonSerializer.Deserialize<AiResultV1>(resultBytes,Json)!;
        Assert.Equal(resultBytes,AiResultCanonicalizer.Encode(result));Assert.Equal(AiManifestCanonicalizer.Hash(manifestBytes),result.ManifestHash);
        using var expected=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(path,"samples.json")));
        Assert.Equal(expected.RootElement.GetProperty(stage+"ManifestHash").GetString(),AiManifestCanonicalizer.Hash(manifestBytes));
        Assert.Equal(expected.RootElement.GetProperty(stage+"ResultHash").GetString(),AiManifestCanonicalizer.Hash(resultBytes));
    }
    private sealed class EnvironmentFixture(string name):IHostEnvironment
    {
        public string EnvironmentName{get;set;}=name;public string ApplicationName{get;set;}="Fixture";public string ContentRootPath{get;set;}=".";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
    }
}
