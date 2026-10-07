using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Processing.Anh02;

public sealed class Anh02AiOptions
{
    public bool MockEnabled { get; set; }
    public string FixtureDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Anh02Fixtures");
}
public interface IAnh02AiService
{
    Task<AiMockRunView> CreateAsync(Guid actor, UserRoleCode role, Guid project, CreateAiMockRunRequest request, string key, CancellationToken ct);
    Task<AiMockRunView> GetAsync(Guid actor, UserRoleCode role, Guid project, Guid run, CancellationToken ct);
    Task<AiResultV1> GetResultAsync(Guid actor, UserRoleCode role, Guid project, Guid run, CancellationToken ct);
    Task<bool> ProcessOneAsync(CancellationToken ct);
}
