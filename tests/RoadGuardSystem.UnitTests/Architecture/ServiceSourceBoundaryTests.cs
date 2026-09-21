using FluentAssertions;
using Xunit;

namespace RoadGuardSystem.UnitTests.Architecture;

[Trait("TaskId", "P1-72")]
public sealed class ServiceSourceBoundaryTests
{
    private static readonly string[] ForbiddenTokens =
    [
        "HttpContext",
        "Microsoft.AspNetCore.Mvc",
        "IActionResult",
        "ControllerBase",
        "RoadGuardDbContext",
        "Microsoft.EntityFrameworkCore"
    ];

    [Fact(DisplayName = "P1-72 service source has no HTTP MVC or DbContext dependency")]
    public void ServiceSource_HasNoHttpMvcOrDbContextDependency()
    {
        var repositoryRoot = FindRepositoryRoot();
        var serviceSourceRoot = Path.Combine(repositoryRoot, "RoadGuardSystem.Services");

        var violations = Directory.EnumerateFiles(serviceSourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildArtifact(path))
            .SelectMany(path => ForbiddenTokens
                .Where(token => File.ReadAllText(path).Contains(token, StringComparison.Ordinal))
                .Select(token => $"{Path.GetRelativePath(repositoryRoot, path)}: {token}"))
            .ToArray();

        violations.Should().BeEmpty(
            because: "Services own use-case policy and must not depend on HTTP/MVC, EF Core, or RoadGuardDbContext");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (directory.EnumerateFiles("*.slnx").Any())
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Cannot locate repository root from the test output directory.");
    }

    private static bool IsBuildArtifact(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Contains("bin", StringComparer.OrdinalIgnoreCase) ||
               segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }
}
