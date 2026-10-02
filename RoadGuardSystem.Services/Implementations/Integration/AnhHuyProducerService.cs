using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;

namespace RoadGuardSystem.Services.Integration;

public sealed class AnhHuyProducerService(IAnhHuyFactsRepository facts, IGeometryWorkflowRepository geometry,
    IProjectScopeGuard guard) : IAnhHuyProducerService
{
    private static AnhHuyProducerResult<T> Fail<T>(AnhHuyProducerStatus status)=>new(status);
    private static ResolvedEvidenceFacts Evidence(ReporterFileFacts f, Guid evidenceId)=>new(
        VerifiedEvidenceReference.Create(evidenceId,f.FileId,f.FileVersion,f.OwnerId),f.ProjectId,f.Purpose,f.Checksum,f.SizeBytes,f.MediaType,f.UploadedAt);
    public async Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePrivateEvidenceAsync(Guid actorId, UserRoleCode role,
        Guid fileId, Guid evidenceId, string? expectedFileVersion=null, CancellationToken cancellationToken=default)
    {
        if(role!=UserRoleCode.Reporter || !await facts.IsCurrentActorAsync(actorId,role,cancellationToken)) return Fail<ResolvedEvidenceFacts>(AnhHuyProducerStatus.Forbidden);
        var file=await facts.GetFileAsync(fileId,cancellationToken);
        if(file is null || file.OwnerId!=actorId || file.ProjectId is not null || file.Purpose!="REPORT_PHOTO") return Fail<ResolvedEvidenceFacts>(AnhHuyProducerStatus.NotFound);
        if(file.State!="VERIFIED") return Fail<ResolvedEvidenceFacts>(AnhHuyProducerStatus.SourceNotReady);
        if(expectedFileVersion is not null && expectedFileVersion!=file.FileVersion) return Fail<ResolvedEvidenceFacts>(AnhHuyProducerStatus.StaleFile);
        if(evidenceId==Guid.Empty) return Fail<ResolvedEvidenceFacts>(AnhHuyProducerStatus.SourceNotReady);
        return new(AnhHuyProducerStatus.Ready,Evidence(file,evidenceId));
    }
    public async Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePublicationEvidenceAsync(Guid actorId, UserRoleCode role,
        Guid publicationId, Guid reportId, Guid evidenceId, CancellationToken cancellationToken=default)
    {
        if(role!=UserRoleCode.Reporter || !await facts.IsCurrentActorAsync(actorId,role,cancellationToken)) return Fail<ResolvedEvidenceFacts>(AnhHuyProducerStatus.Forbidden);
        var id=await facts.GetPublicationFileAsync(actorId,publicationId,reportId,evidenceId,cancellationToken);
        if(id is null) return Fail<ResolvedEvidenceFacts>(AnhHuyProducerStatus.NotFound);
        var file=await facts.GetFileAsync(id.FileId,cancellationToken);
        if(file is null) return Fail<ResolvedEvidenceFacts>(AnhHuyProducerStatus.NotFound);
        if(file.State!="VERIFIED" || file.FileVersion!=id.FileVersion || file.OwnerId!=id.OwnerUserId
            || file.ProjectId is not null || file.Purpose!="REPORT_PHOTO") return Fail<ResolvedEvidenceFacts>(AnhHuyProducerStatus.SourceNotReady);
        return new(AnhHuyProducerStatus.Ready,Evidence(file,evidenceId));
    }
    public async Task<AnhHuyProducerResult<ProjectGeometryContext>> ResolveGeometryAsync(Guid actorId, UserRoleCode role,
        Guid projectId, Guid routeVersionId, Guid segmentSetId, string? expectedVersion=null, bool requireCurrent=true,
        CancellationToken cancellationToken=default)
    {
        if(role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) || !await facts.IsCurrentActorAsync(actorId,role,cancellationToken)
            || await guard.AuthorizeAsync(actorId,role,projectId,cancellationToken) is null) return Fail<ProjectGeometryContext>(AnhHuyProducerStatus.Forbidden);
        var result=await geometry.ExecuteAsync(new GeometryWorkflowCommand(actorId,projectId,"package",null,null,routeVersionId,segmentSetId,null,null,null),
            RoadGuardSystem.Services.Projects.GeometryEngine.Preview,RoadGuardSystem.Services.Projects.GeometryEngine.Segments,cancellationToken);
        if(result.Value is not GeometryPackageView package) return Fail<ProjectGeometryContext>(AnhHuyProducerStatus.NotFound);
        if(package.Route.MetadataStatus!="COMPLETE" || package.SegmentSet.MetadataStatus!="COMPLETE") return Fail<ProjectGeometryContext>(AnhHuyProducerStatus.SourceNotReady);
        if((requireCurrent && (!package.Route.IsCurrent || package.SegmentSet.Status!="PUBLISHED")) || (expectedVersion is not null && expectedVersion!=result.Version))
            return Fail<ProjectGeometryContext>(AnhHuyProducerStatus.StaleGeometry);
        var ordered=package.SegmentSet.Segments.OrderBy(s=>s.Sequence).ToArray();
        var adjacency=ordered.Select((s,i)=>new GeometrySegmentContext(s.Id,s.Sequence,i==0?null:ordered[i-1].Id,i==ordered.Length-1?null:ordered[i+1].Id)).ToArray();
        return new(AnhHuyProducerStatus.Ready,new("anh-huy.geometry.v1",projectId,routeVersionId,segmentSetId,result.Version!,package,adjacency));
    }
    public async Task<AnhHuyProducerResult<ResolvedCandidateSourceFacts>> ResolveCandidateSourceAsync(Guid actorId, UserRoleCode role,
        Guid projectId, CandidateSourceKind kind, Guid sourceId, string? expectedSourceVersion=null,
        string? expectedGeometryVersion=null, string? expectedDispositionVersion=null, CancellationToken cancellationToken=default)
    {
        if(role!=UserRoleCode.ProjectManager || !await facts.IsCurrentActorAsync(actorId,role,cancellationToken)
            || await guard.AuthorizeAsync(actorId,role,projectId,cancellationToken) is null) return Fail<ResolvedCandidateSourceFacts>(AnhHuyProducerStatus.Forbidden);
        // No accepted AI/Field source-provenance producer exists in this integration.
        if(kind!=CandidateSourceKind.Report) return Fail<ResolvedCandidateSourceFacts>(AnhHuyProducerStatus.SourceNotReady);
        var source=await facts.GetReportSourceAsync(sourceId,cancellationToken);
        if(source is null) return Fail<ResolvedCandidateSourceFacts>(AnhHuyProducerStatus.NotFound);
        if(source.ProjectId is not null && source.ProjectId!=projectId) return Fail<ResolvedCandidateSourceFacts>(AnhHuyProducerStatus.NotFound);
        if(source.ProjectId is null || source.RouteVersionId is null || source.SegmentSetId is null || source.Evidence.Length==0)
            return Fail<ResolvedCandidateSourceFacts>(AnhHuyProducerStatus.SourceNotReady);
        var context=await ResolveGeometryAsync(actorId,role,projectId,source.RouteVersionId.Value,source.SegmentSetId.Value,expectedGeometryVersion,true,cancellationToken);
        if(context.Status!=AnhHuyProducerStatus.Ready) return Fail<ResolvedCandidateSourceFacts>(context.Status);
        if(expectedDispositionVersion is not null && expectedDispositionVersion!=source.Disposition?.Version)
            return Fail<ResolvedCandidateSourceFacts>(AnhHuyProducerStatus.StaleDisposition);
        var files=new List<ResolvedEvidenceFacts>();
        foreach(var item in source.Evidence.OrderBy(e=>e.EvidenceId))
        {
            var file=await facts.GetFileAsync(item.FileId,cancellationToken);
            if(file is null || file.State!="VERIFIED" || file.OwnerId!=source.OwnerId || file.ProjectId is not null || file.Purpose!="REPORT_PHOTO"
                || file.FileVersion!=item.FileVersion) return Fail<ResolvedCandidateSourceFacts>(AnhHuyProducerStatus.SourceNotReady);
            files.Add(Evidence(file,item.EvidenceId));
        }
        var version=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {source.ReportId,source.ReportVersion,
            source.CaseId,source.CaseVersion,source.DispositionHeadVersion,geometry=context.Facts!.Version,
            evidence=files.Select(f=>new{f.Reference.EvidenceId,f.Reference.FileId,f.Reference.FileVersion,f.ChecksumSha256})}))).ToLowerInvariant();
        if(expectedSourceVersion is not null && expectedSourceVersion!=version) return Fail<ResolvedCandidateSourceFacts>(AnhHuyProducerStatus.StaleSource);
        var domain=CandidateSourceFacts.Create(CandidateSourceIdentity.Create(kind,sourceId,version),projectId,context.Facts!.Version,source.Disposition);
        return new(AnhHuyProducerStatus.Ready,new(domain,source.CaseId,files.Select(f=>f.Reference.EvidenceId).ToArray(),files.ToArray(),context.Facts));
    }
}
