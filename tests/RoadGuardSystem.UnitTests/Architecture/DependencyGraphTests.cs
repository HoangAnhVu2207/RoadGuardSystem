using FluentAssertions;
using Xunit;
using static RoadGuardSystem.UnitTests.Architecture.DependencyGraphChecker;

namespace RoadGuardSystem.UnitTests.Architecture;

/// <summary>
/// Architecture dependency-graph tests for P1-00.
///
/// NEGATIVE-FIRST APPROACH:
///   Phase 1 — Fixture tests prove that the checker DETECTS forbidden edges
///              on deliberately wrong graphs. These tests verify the checker
///              itself works before trusting the production-graph assertions.
///   Phase 2 — Production tests apply the same checker to the real .csproj files
///              and assert no violations exist.
///
/// No forbidden edge is added to any production .csproj file to make a test "red".
/// Instead, in-memory fixture graphs supply the wrong edges.
///
/// Evidence of red-first: the fixture tests pass (checker detects the bad edges),
/// while the production tests pass (no bad edges in production). If someone
/// introduced a forbidden edge in production, the production tests would turn red.
/// </summary>
[Trait("TaskId", "P1-00")]
public sealed class DependencyGraphTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Resolves the repository root relative to the test binary output directory.
    /// The test project sits at: tests/RoadGuardSystem.UnitTests/
    /// The repo root is 3 levels up from bin/Debug/net8.0 → ../../.. → tests/ → root.
    /// Adjust depth if layout changes.
    /// </summary>
    private static string RepositoryRoot()
    {
        // Walk up from the current assembly's location until we find the .slnx file.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.slnx").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            "Cannot locate repository root: no *.slnx file found walking up from " +
            AppContext.BaseDirectory);
    }

    private static DependencyGraph BuildProductionGraph()
    {
        var root = RepositoryRoot();

        // Map short logical names to actual csproj paths.
        var projects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BusinessObjects"] = Path.Combine(root, "RoadGuardSystem.BusinessObjects", "RoadGuardSystem.aBusinessObjects.csproj"),
            ["DTOs"] = Path.Combine(root, "RoadGuardSystem.DTOs", "RoadGuardSystem.bDTOs.csproj"),
            ["Repositories"] = Path.Combine(root, "RoadGuardSystem.Repositories", "RoadGuardSystem.cRepositories.csproj"),
            ["Services"] = Path.Combine(root, "RoadGuardSystem.Services", "RoadGuardSystem.dServices.csproj"),
            ["API"] = Path.Combine(root, "RoadGuardSystem.API", "RoadGuardSystem.eAPI.csproj"),
        };

        // Map the csproj filename (without extension) → logical short name so that
        // ProjectReference values are normalised.
        var assemblyToLogical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["RoadGuardSystem.aBusinessObjects"] = "BusinessObjects",
            ["RoadGuardSystem.bDTOs"] = "DTOs",
            ["RoadGuardSystem.cRepositories"] = "Repositories",
            ["RoadGuardSystem.dServices"] = "Services",
            ["RoadGuardSystem.eAPI"] = "API",
        };

        var edges = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (logical, csprojPath) in projects)
        {
            var rawRefs = ReadDirectReferences(csprojPath);
            var logicalRefs = rawRefs
                .Where(r => assemblyToLogical.ContainsKey(r))
                .Select(r => assemblyToLogical[r])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            edges[logical] = logicalRefs;
        }

        return new DependencyGraph(edges);
    }

    // ===========================================================================
    // PHASE 1 — NEGATIVE: fixture graphs prove the checker detects bad edges
    // ===========================================================================

    [Fact(DisplayName = "Checker detects BusinessObjects -> Services violation in fixture graph")]
    public void Checker_Detects_BusinessObjects_DependsOn_Services()
    {
        // ARRANGE — deliberately wrong graph: BusinessObjects -> Services
        var badGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string> { "Services" },
            ["Services"] = new HashSet<string> { "Repositories" },
            ["Repositories"] = new HashSet<string> { "DTOs" },
            ["DTOs"] = new HashSet<string> { "BusinessObjects" }, // legal in real, kept for symmetry
            ["API"] = new HashSet<string> { "Services" },
        });

        // ACT
        var violations = FindForbiddenEdges(badGraph);

        // ASSERT — checker must fire at least the BusinessObjects -> Services rule
        violations.Should().Contain(v => v.Contains("BusinessObjects", StringComparison.OrdinalIgnoreCase)
                                      && v.Contains("Services", StringComparison.OrdinalIgnoreCase),
            because: "the checker must detect a BusinessObjects -> Services dependency");
    }

    [Fact(DisplayName = "Checker detects API -> Repositories violation in fixture graph")]
    public void Checker_Detects_API_DependsOn_Repositories()
    {
        // ARRANGE — API directly references Repositories (forbidden)
        var badGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string>(),
            ["DTOs"] = new HashSet<string> { "BusinessObjects" },
            ["Repositories"] = new HashSet<string> { "DTOs" },
            ["Services"] = new HashSet<string> { "Repositories" },
            ["API"] = new HashSet<string> { "Services", "Repositories" }, // forbidden
        });

        // ACT
        var violations = FindForbiddenEdges(badGraph);

        // ASSERT
        violations.Should().Contain(v => v.Contains("API", StringComparison.OrdinalIgnoreCase)
                                      && v.Contains("Repositories", StringComparison.OrdinalIgnoreCase),
            because: "the checker must detect API -> Repositories direct dependency");
    }

    [Fact(DisplayName = "Checker detects Services -> API violation in fixture graph")]
    public void Checker_Detects_Services_DependsOn_API()
    {
        // ARRANGE — Services references API (forbidden)
        var badGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string>(),
            ["DTOs"] = new HashSet<string> { "BusinessObjects" },
            ["Repositories"] = new HashSet<string> { "DTOs" },
            ["Services"] = new HashSet<string> { "Repositories", "API" }, // forbidden
            ["API"] = new HashSet<string> { "Services" },
        });

        // ACT
        var violations = FindForbiddenEdges(badGraph);

        // ASSERT
        violations.Should().Contain(v => v.Contains("Services", StringComparison.OrdinalIgnoreCase)
                                      && v.Contains("API", StringComparison.OrdinalIgnoreCase),
            because: "the checker must detect Services -> API dependency");
    }

    [Fact(DisplayName = "Checker detects circular dependency A -> B -> A in fixture graph")]
    public void Checker_Detects_Circular_Dependency()
    {
        // ARRANGE — circular: Alpha -> Beta -> Alpha
        var cyclicGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["Alpha"] = new HashSet<string> { "Beta" },
            ["Beta"] = new HashSet<string> { "Alpha" }, // cycle back
        });

        // ACT
        var violations = FindForbiddenEdges(cyclicGraph);

        // ASSERT
        violations.Should().Contain(v => v.Contains("CIRCULAR", StringComparison.OrdinalIgnoreCase),
            because: "the checker must detect A -> B -> A circular dependency");
    }

    [Fact(DisplayName = "Checker detects BusinessObjects -> Repositories violation in fixture graph")]
    public void Checker_Detects_BusinessObjects_DependsOn_Repositories()
    {
        var badGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string> { "Repositories" }, // forbidden
            ["Repositories"] = new HashSet<string>(),
            ["DTOs"] = new HashSet<string>(),
            ["Services"] = new HashSet<string>(),
            ["API"] = new HashSet<string>(),
        });

        var violations = FindForbiddenEdges(badGraph);

        violations.Should().Contain(v => v.Contains("BusinessObjects", StringComparison.OrdinalIgnoreCase)
                                      && v.Contains("Repositories", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Checker detects BusinessObjects -> API violation in fixture graph")]
    public void Checker_Detects_BusinessObjects_DependsOn_API()
    {
        var badGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string> { "API" }, // forbidden
            ["API"] = new HashSet<string>(),
            ["DTOs"] = new HashSet<string>(),
            ["Repositories"] = new HashSet<string>(),
            ["Services"] = new HashSet<string>(),
        });

        var violations = FindForbiddenEdges(badGraph);

        violations.Should().Contain(v => v.Contains("BusinessObjects", StringComparison.OrdinalIgnoreCase)
                                      && v.Contains("API", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Checker detects DTOs -> Services violation in fixture graph")]
    public void Checker_Detects_DTOs_DependsOn_Services()
    {
        var badGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string>(),
            ["DTOs"] = new HashSet<string> { "Services" }, // forbidden
            ["Services"] = new HashSet<string>(),
            ["Repositories"] = new HashSet<string>(),
            ["API"] = new HashSet<string>(),
        });

        var violations = FindForbiddenEdges(badGraph);

        violations.Should().Contain(v => v.Contains("DTOs", StringComparison.OrdinalIgnoreCase)
                                      && v.Contains("Services", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Checker detects DTOs -> API violation in fixture graph")]
    public void Checker_Detects_DTOs_DependsOn_API()
    {
        var badGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string>(),
            ["DTOs"] = new HashSet<string> { "API" }, // forbidden
            ["Services"] = new HashSet<string>(),
            ["Repositories"] = new HashSet<string>(),
            ["API"] = new HashSet<string>(),
        });

        var violations = FindForbiddenEdges(badGraph);

        violations.Should().Contain(v => v.Contains("DTOs", StringComparison.OrdinalIgnoreCase)
                                      && v.Contains("API", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Checker detects Repositories -> Services violation in fixture graph")]
    public void Checker_Detects_Repositories_DependsOn_Services()
    {
        var badGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string>(),
            ["DTOs"] = new HashSet<string>(),
            ["Repositories"] = new HashSet<string> { "Services" }, // forbidden
            ["Services"] = new HashSet<string>(),
            ["API"] = new HashSet<string>(),
        });

        var violations = FindForbiddenEdges(badGraph);

        violations.Should().Contain(v => v.Contains("Repositories", StringComparison.OrdinalIgnoreCase)
                                      && v.Contains("Services", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Checker detects Repositories -> API violation in fixture graph")]
    public void Checker_Detects_Repositories_DependsOn_API()
    {
        var badGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string>(),
            ["DTOs"] = new HashSet<string>(),
            ["Repositories"] = new HashSet<string> { "API" }, // forbidden
            ["Services"] = new HashSet<string>(),
            ["API"] = new HashSet<string>(),
        });

        var violations = FindForbiddenEdges(badGraph);

        violations.Should().Contain(v => v.Contains("Repositories", StringComparison.OrdinalIgnoreCase)
                                      && v.Contains("API", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Clean fixture graph with no forbidden edges passes checker")]
    public void Checker_Passes_Clean_Fixture_Graph()
    {
        // ARRANGE — a perfectly layered graph matching the allowed architecture
        var cleanGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string>(),
            ["DTOs"] = new HashSet<string> { "BusinessObjects" },
            ["Repositories"] = new HashSet<string> { "DTOs" },
            ["Services"] = new HashSet<string> { "Repositories" },
            ["API"] = new HashSet<string> { "Services" },
        });

        var violations = FindForbiddenEdges(cleanGraph);

        violations.Should().BeEmpty(because: "this graph has no forbidden edges");
    }

    // ===========================================================================
    // PHASE 1 (F1/F2) — NEGATIVE: fixture package sets prove checker detects violations
    // ===========================================================================

    [Fact(DisplayName = "Checker detects EF Identity package violation in BusinessObjects fixture")]
    public void Checker_Detects_EFIdentity_PackageIn_BusinessObjects()
    {
        // ARRANGE — fixture package set containing the forbidden EF Identity package
        var badPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.AspNetCore.Identity.EntityFrameworkCore",
            "SomeOtherLegitPackage",
        };

        // ACT
        var violations = FindForbiddenPackages(
            "BusinessObjects", badPackages, ForbiddenBusinessObjectsPackagePrefixes);

        // ASSERT — checker must detect the forbidden EF Identity package
        violations.Should().NotBeEmpty(
            because: "Microsoft.AspNetCore.Identity.EntityFrameworkCore is a forbidden EF persistence package in BusinessObjects");
        violations.Should().Contain(v =>
            v.Contains("Microsoft.AspNetCore.Identity.EntityFrameworkCore",
                StringComparison.OrdinalIgnoreCase),
            because: "the checker must name the specific forbidden package");
    }

    [Fact(DisplayName = "Checker detects EF Core package violation in BusinessObjects fixture")]
    public void Checker_Detects_EFCore_PackageIn_BusinessObjects()
    {
        // ARRANGE
        var badPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.EntityFrameworkCore",
            "Microsoft.EntityFrameworkCore.SqlServer",
        };

        // ACT
        var violations = FindForbiddenPackages(
            "BusinessObjects", badPackages, ForbiddenBusinessObjectsPackagePrefixes);

        // ASSERT
        violations.Should().NotBeEmpty(
            because: "EF Core packages are not allowed in BusinessObjects per AGENTS.md");
    }

    [Fact(DisplayName = "Checker detects EF Core package violation in DTOs fixture")]
    public void Checker_Detects_EFCore_PackageIn_DTOs()
    {
        // ARRANGE
        var badPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.EntityFrameworkCore.SqlServer",
            "Microsoft.EntityFrameworkCore.Design",
        };

        // ACT
        var violations = FindForbiddenPackages(
            "DTOs", badPackages, ForbiddenDTOsPackagePrefixes);

        // ASSERT
        violations.Should().NotBeEmpty(
            because: "EF Core packages must not appear in DTOs — they belong in Repositories");
    }

    [Fact(DisplayName = "Checker detects HTTP package violation in DTOs fixture")]
    public void Checker_Detects_HTTP_PackageIn_DTOs()
    {
        // ARRANGE
        var badPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.AspNetCore.Http",
            "Microsoft.AspNetCore.Http.Abstractions",
        };

        // ACT
        var violations = FindForbiddenPackages(
            "DTOs", badPackages, ForbiddenDTOsPackagePrefixes);

        // ASSERT
        violations.Should().NotBeEmpty(
            because: "Microsoft.AspNetCore.Http is a transport package forbidden in DTOs");
    }

    [Fact(DisplayName = "Checker detects Configuration package violation in DTOs fixture")]
    public void Checker_Detects_Configuration_PackageIn_DTOs()
    {
        // ARRANGE
        var badPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.Extensions.Configuration.Json",
            "Microsoft.Extensions.Configuration.UserSecrets",
        };

        // ACT
        var violations = FindForbiddenPackages(
            "DTOs", badPackages, ForbiddenDTOsPackagePrefixes);

        // ASSERT
        violations.Should().NotBeEmpty(
            because: "Configuration packages are infrastructure concerns that must not appear in DTOs");
    }

    [Fact(DisplayName = "Clean package set in BusinessObjects passes package checker")]
    public void Checker_Passes_Clean_BusinessObjects_Packages()
    {
        // ARRANGE — only allowed packages (domain utilities, no EF/transport)
        var cleanPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.Extensions.Logging.Abstractions",
            "System.Text.Json",
        };

        // ACT
        var violations = FindForbiddenPackages(
            "BusinessObjects", cleanPackages, ForbiddenBusinessObjectsPackagePrefixes);

        // ASSERT
        violations.Should().BeEmpty(because: "none of these packages are forbidden in BusinessObjects");
    }

    [Fact(DisplayName = "Clean package set in DTOs passes package checker")]
    public void Checker_Passes_Clean_DTOs_Packages()
    {
        // ARRANGE — only contract packages
        var cleanPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "System.ComponentModel.Annotations",
            "Microsoft.Extensions.Logging.Abstractions",
        };

        // ACT
        var violations = FindForbiddenPackages(
            "DTOs", cleanPackages, ForbiddenDTOsPackagePrefixes);

        // ASSERT
        violations.Should().BeEmpty(because: "none of these packages are forbidden in DTOs");
    }

    [Fact(DisplayName = "Checker detects BusinessObjects -> DTOs project reference violation in fixture graph")]
    public void Checker_Detects_BusinessObjects_DependsOn_DTOs()
    {
        // ARRANGE — BusinessObjects references DTOs (forbidden)
        var badGraph = new DependencyGraph(new Dictionary<string, IReadOnlySet<string>>
        {
            ["BusinessObjects"] = new HashSet<string> { "DTOs" }, // forbidden
            ["DTOs"] = new HashSet<string>(),
            ["Repositories"] = new HashSet<string>(),
            ["Services"] = new HashSet<string>(),
            ["API"] = new HashSet<string>(),
        });

        // ACT
        var violations = FindForbiddenEdges(badGraph);

        // ASSERT
        violations.Should().Contain(
            v => v.Contains("BusinessObjects", StringComparison.OrdinalIgnoreCase)
              && v.Contains("DTOs", StringComparison.OrdinalIgnoreCase),
            because: "the checker must detect BusinessObjects -> DTOs project reference");
    }

    // ===========================================================================
    // PHASE 2 (F1/F2) — PRODUCTION package boundary tests
    //
    // EXPECTED INITIAL STATE (negative-first evidence for F1/F2):
    //   - BusinessObjects_HasNoForbiddenPackages FAILS because
    //     RoadGuardSystem.aBusinessObjects.csproj contains
    //     "Microsoft.AspNetCore.Identity.EntityFrameworkCore".
    //   - DTOs_HasNoForbiddenPackages FAILS because
    //     RoadGuardSystem.bDTOs.csproj contains EF Core and HTTP packages.
    //   These are the EXPECTED RED states before production .csproj cleanup.
    //   After cleanup, both tests must turn green.
    // ===========================================================================

    [Fact(DisplayName = "BusinessObjects production project has no forbidden package references (F1)")]
    public void BusinessObjects_HasNoForbiddenPackages()
    {
        var root = RepositoryRoot();
        var csprojPath = Path.Combine(
            root, "RoadGuardSystem.BusinessObjects", "RoadGuardSystem.aBusinessObjects.csproj");

        var packages = ReadPackageReferences(csprojPath);
        var violations = FindForbiddenPackages(
            "BusinessObjects", packages, ForbiddenBusinessObjectsPackagePrefixes);

        violations.Should().BeEmpty(
            because: "BusinessObjects must not contain EF Core or EF Identity packages per AGENTS.md: " +
                     "'BusinessObjects has no dependency on EF Core or transport details'");
    }

    [Fact(DisplayName = "DTOs production project has no forbidden package references (F2)")]
    public void DTOs_HasNoForbiddenPackages()
    {
        var root = RepositoryRoot();
        var csprojPath = Path.Combine(
            root, "RoadGuardSystem.DTOs", "RoadGuardSystem.bDTOs.csproj");

        var packages = ReadPackageReferences(csprojPath);
        var violations = FindForbiddenPackages(
            "DTOs", packages, ForbiddenDTOsPackagePrefixes);

        violations.Should().BeEmpty(
            because: "DTOs must contain only public contract packages — EF Core, HTTP, and " +
                     "Configuration packages belong in Repositories or API per AGENTS.md");
    }

    // ===========================================================================
    // PHASE 2 (original) — POSITIVE: production ProjectReference graph
    // ===========================================================================

    [Fact(DisplayName = "Production dependency graph satisfies all architecture rules")]
    public void Production_DependencyGraph_HasNoForbiddenEdges()
    {
        // ARRANGE — read from real .csproj files
        var productionGraph = BuildProductionGraph();

        // ACT
        var violations = FindForbiddenEdges(productionGraph);

        // ASSERT
        violations.Should().BeEmpty(
            because: "the production dependency graph must comply with the architecture rules defined in AGENTS.md");
    }

    [Fact(DisplayName = "Production graph has no circular dependencies")]
    public void Production_DependencyGraph_HasNoCycles()
    {
        var productionGraph = BuildProductionGraph();
        var cycle = FindCycle(productionGraph);

        cycle.Should().BeNull(
            because: "no circular ProjectReference must exist in the production graph");
    }
}
