using FluentAssertions;
using Xunit;

namespace RoadGuardSystem.UnitTests.Architecture;

[Trait("TaskId", "P1-72")]
public sealed class RepositorySourceBoundaryTests
{
    [Fact(DisplayName = "P1-72 repository source does not reference DTO namespace")]
    public void RepositorySource_HasNoDtoNamespaceReference()
    {
        var repositoryRoot = FindRepositoryRoot();
        var repositorySourceRoot = Path.Combine(repositoryRoot, "RoadGuardSystem.Repositories");

        var violations = Directory.EnumerateFiles(repositorySourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildArtifact(path))
            .Where(path => File.ReadAllText(path).Contains("RoadGuardSystem.DTOs", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        violations.Should().BeEmpty(
            because: "ADR 001 permits the Repositories-to-DTOs project reference, but Repository source must not depend on DTO types");
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
