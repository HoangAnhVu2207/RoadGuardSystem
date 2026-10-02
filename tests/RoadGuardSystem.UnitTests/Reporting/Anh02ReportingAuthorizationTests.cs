using System.Reflection;
using FluentAssertions;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Reporting;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Reporting;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reporting;

public sealed class Anh02ReportingAuthorizationTests
{
    [Theory]
    [InlineData(UserRoleCode.RepairCrew, UserStatus.Active, false)]
    [InlineData(UserRoleCode.Reporter, UserStatus.Active, false)]
    [InlineData(UserRoleCode.DroneOperator, UserStatus.Active, false)]
    [InlineData(UserRoleCode.ProjectManager, UserStatus.Suspended, false)]
    [InlineData(UserRoleCode.ProjectManager, UserStatus.Active, true)]
    public async Task CurrentSqlActorDeniesBeforeFactsRead(UserRoleCode role, UserStatus status, bool mustChange)
    {
        var identity = DispatchProxy.Create<IIdentityRepository, IdentityProxy>();
        ((IdentityProxy)(object)identity).State = new(Guid.NewGuid(), "fixture", "fixture", role, status, mustChange, []);
        var repository = new Repository(); var guard = new Guard();
        var service = new ReportingService(repository, identity, guard, TimeProvider.System);
        var result = await service.SummaryAsync(Guid.NewGuid(), Guid.NewGuid(), new(), default);
        result.Code.Should().Be("access_forbidden"); repository.Captured.Should().BeFalse(); guard.Called.Should().BeFalse();
    }

    [Fact]
    public async Task RevokedMembershipDeniesEvenWhenCurrentSqlUserIsManager()
    {
        var identity = DispatchProxy.Create<IIdentityRepository, IdentityProxy>();
        ((IdentityProxy)(object)identity).State = new(Guid.NewGuid(), "fixture", "fixture", UserRoleCode.ProjectManager, UserStatus.Active, false, []);
        var repository = new Repository(); var guard = new Guard();
        var result = await new ReportingService(repository, identity, guard, TimeProvider.System).SummaryAsync(Guid.NewGuid(), Guid.NewGuid(), new(), default);
        result.Code.Should().Be("access_forbidden"); repository.Captured.Should().BeFalse(); guard.Called.Should().BeTrue();
    }

    [Theory]
    [InlineData("2026-10-01T00:00:00Z", "2026-10-01T00:00:00Z")]
    [InlineData("2026-10-02T00:00:00Z", "2026-10-01T00:00:00Z")]
    [InlineData("2026-10-01T00:00:00+07:00", "2026-10-02T00:00:00+07:00")]
    public void InvalidPeriodsCannotPretendToBeHistoricalSnapshots(string from, string to) =>
        ReportingService.TryNormalize(new(DateTimeOffset.Parse(from), DateTimeOffset.Parse(to)), out _).Should().BeFalse();

    public class IdentityProxy : DispatchProxy
    {
        public UserSecurityState? State { get; set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            nameof(IIdentityRepository.GetUserSecurityStateAsync) => Task.FromResult(State),
            nameof(IIdentityRepository.IsRoleActiveAsync) => Task.FromResult(true),
            _ => throw new NotSupportedException()
        };
    }
    private sealed class Guard : IProjectScopeGuard
    {
        public bool Called { get; private set; }
        public Task<ProjectAccessScope?> AuthorizeAsync(Guid userId, UserRoleCode authoritativeRole, Guid projectId, CancellationToken cancellationToken = default)
        { Called = true; return Task.FromResult<ProjectAccessScope?>(null); }
    }
    private sealed class Repository : IReportingRepository
    {
        public bool Captured { get; private set; }
        public Task<T> ReadConsistentlyAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken token) => read(token);
        public Task<ReportingReadResult> CaptureAsync(Guid project, ReportingFiltersDto filters, CancellationToken token)
        { Captured = true; throw new InvalidOperationException("Authorization must precede facts."); }
        public Task<bool> AggregateBelongsAsync(Guid project, string type, Guid id, CancellationToken token) => throw new NotSupportedException();
        public Task<ReportingTimelineItemDto[]> TimelineAsync(string type, Guid id, ReportingFiltersDto filters, DateTimeOffset? afterTime, Guid? afterId, int take, CancellationToken token) => throw new NotSupportedException();
    }
}
