using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.DTOs.Files;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Files;
using RoadGuardSystem.Services.Implementations.Files;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Files;

public sealed class Anh01UploadAdmissionTests
{
    [Theory]
    [InlineData("SURVEY_VIDEO", "video/mp4", 8589934592L, true)]
    [InlineData("SURVEY_VIDEO", "video/mp4", 8589934591L, true)]
    [InlineData("SURVEY_VIDEO", "video/mp4", 8589934593L, false)]
    [InlineData("TELEMETRY", "application/x-subrip", 10485760L, true)]
    [InlineData("TELEMETRY", "application/x-subrip", 10485759L, true)]
    [InlineData("TELEMETRY", "application/x-subrip", 10485761L, false)]
    [InlineData("BEFORE", "image/jpeg", 20971520L, true)]
    [InlineData("BEFORE", "image/jpeg", 20971519L, true)]
    [InlineData("BEFORE", "image/jpeg", 0L, false)]
    [InlineData("BEFORE", "image/jpeg", -1L, false)]
    [InlineData("BEFORE", "image/jpeg", 20971521L, false)]
    [InlineData("BEFORE", "video/mp4", 100L, false)]
    [InlineData("DOCUMENT", "application/pdf", 2147483648L, false)]
    public async Task Admission_EnforcesPurposeAndSizeBeforePersistence(string purpose, string mime, long bytes, bool allowed)
    {
        var repo = DispatchProxy.Create<IUploadRepository, RepositoryProxy>();
        var guard = DispatchProxy.Create<IProjectScopeGuard, GuardProxy>();
        var service = new UploadService(repo, guard, TimeProvider.System, Options.Create(new UploadSessionOptions()));
        var result = await service.CreateAsync(Guid.NewGuid(), purpose == "BEFORE" ? UserRoleCode.RepairCrew : UserRoleCode.DroneOperator,
            new(purpose, Guid.NewGuid(), Guid.NewGuid(), "fixture", mime, bytes, new string('a', 64)), "key", null);
        result.Status.Should().Be(allowed ? UploadServiceStatus.Success : UploadServiceStatus.InvalidInput);
        ((RepositoryProxy)(object)repo).Writes.Should().Be(allowed ? 1 : 0);
    }

    public class GuardProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Task.FromResult<ProjectAccessScope?>(new((Guid)args![2]!, UserRoleCode.ProjectManager, Guid.NewGuid()));
    }
    public class RepositoryProxy : DispatchProxy
    {
        public int Writes { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name is "IsCurrentSurveyOperatorAsync" or "IsCurrentFieldActorAsync") return Task.FromResult(true);
            Writes++;
            return Task.FromResult(new UploadMutationPersistenceResult(UploadPersistenceStatus.Success,
                new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "opaque", "CREATED", 8388608, DateTimeOffset.UtcNow.AddHours(24), "version")));
        }
    }
}
