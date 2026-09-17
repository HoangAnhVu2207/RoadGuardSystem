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

        return BuildGraph(projects, assemblyToLogical, ReadDirectReferences);
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

    [Fact(DisplayName = "Checker detects legacy ASP.NET Core Identity package in Services fixture")]
    public void Checker_Detects_Legacy_AspNetCore_Identity_Package_In_Services()
    {
        var badPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.AspNetCore.Identity",
        };

        var violations = FindForbiddenPackages(
            "Services", badPackages, ForbiddenServicesPackagePrefixes);

        violations.Should().ContainSingle()
            .Which.Should().Contain("Microsoft.AspNetCore.Identity");
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

    [Fact(DisplayName = "Production graph policy rejects unmapped ProjectReference from BusinessObjects")]
    public void ProductionGraphPolicy_Rejects_Unmapped_ProjectReference_From_BusinessObjects()
    {
        // ARRANGE — BusinessObjects has a reference to an unmapped infrastructure project
        var projects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BusinessObjects"] = "RoadGuardSystem.BusinessObjects",
            ["DTOs"] = "RoadGuardSystem.DTOs",
            ["Repositories"] = "RoadGuardSystem.Repositories",
            ["Services"] = "RoadGuardSystem.Services",
            ["API"] = "RoadGuardSystem.API",
        };

        var assemblyToLogical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["RoadGuardSystem.aBusinessObjects"] = "BusinessObjects",
            ["RoadGuardSystem.bDTOs"] = "DTOs",
            ["RoadGuardSystem.cRepositories"] = "Repositories",
            ["RoadGuardSystem.dServices"] = "Services",
            ["RoadGuardSystem.eAPI"] = "API",
        };

        // Simulated reader: BusinessObjects references an unmapped project
        var rawReferences = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["RoadGuardSystem.BusinessObjects"] = new HashSet<string> { "RoadGuardSystem.NewInfrastructure" },
            ["RoadGuardSystem.DTOs"] = new HashSet<string> { "RoadGuardSystem.aBusinessObjects" },
            ["RoadGuardSystem.Repositories"] = new HashSet<string> { "RoadGuardSystem.bDTOs" },
            ["RoadGuardSystem.Services"] = new HashSet<string> { "RoadGuardSystem.cRepositories" },
            ["RoadGuardSystem.API"] = new HashSet<string> { "RoadGuardSystem.dServices" },
        };

        // ACT — Build graph and evaluate forbidden edges
        var graph = BuildGraph(projects, assemblyToLogical, path => rawReferences[path]);
        var violations = FindForbiddenEdges(graph);

        // ASSERT — Policy must not silently drop the unmapped reference; it must reject it
        violations.Should().NotBeEmpty(
            because: "an unmapped ProjectReference from BusinessObjects must be rejected by production graph policy");
        violations.Should().Contain(v =>
            v.Contains("BusinessObjects", StringComparison.OrdinalIgnoreCase) &&
            v.Contains("RoadGuardSystem.NewInfrastructure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Checker detects direct HTTP transport package in BusinessObjects fixture")]
    public void Checker_Detects_Direct_Http_TransportPackage_In_BusinessObjects()
    {
        var badPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.AspNetCore.Http",
        };

        var violations = FindForbiddenPackages(
            "BusinessObjects", badPackages, ForbiddenBusinessObjectsPackagePrefixes);

        violations.Should().NotBeEmpty(
            because: "transport packages such as Microsoft.AspNetCore.Http are forbidden in BusinessObjects");
        violations.Should().Contain(v =>
            v.Contains("Microsoft.AspNetCore.Http", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Checker detects direct JWT package in BusinessObjects fixture")]
    public void Checker_Detects_Direct_Jwt_Package_In_BusinessObjects()
    {
        var badPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "System.IdentityModel.Tokens.Jwt",
        };

        var violations = FindForbiddenPackages(
            "BusinessObjects", badPackages, ForbiddenBusinessObjectsPackagePrefixes);

        violations.Should().NotBeEmpty(
            because: "JWT packages are forbidden in BusinessObjects");
        violations.Should().Contain(v =>
            v.Contains("System.IdentityModel.Tokens.Jwt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Checker detects transitive EF Core in resolved package graph fixture")]
    public void Checker_Detects_Transitive_EFCore_In_ResolvedPackages()
    {
        var syntheticAssets = """
        {
          "version": 3,
          "targets": {
            "net8.0": {
              "Microsoft.Extensions.Identity.Stores/8.0.17": {
                "type": "package",
                "dependencies": {
                  "Microsoft.EntityFrameworkCore": "8.0.17"
                }
              },
              "Microsoft.EntityFrameworkCore/8.0.17": {
                "type": "package"
              }
            }
          },
          "libraries": {
            "Microsoft.Extensions.Identity.Stores/8.0.17": {
              "type": "package"
            },
            "Microsoft.EntityFrameworkCore/8.0.17": {
              "type": "package"
            }
          }
        }
        """;

        var resolved = ParseResolvedPackages(syntheticAssets);
        var violations = FindForbiddenPackages(
            "BusinessObjects (resolved)", resolved, ForbiddenTransitiveBusinessObjectsPackagePrefixes);

        violations.Should().NotBeEmpty(
            because: "transitive EF Core package in resolved graph must be detected");
        violations.Should().Contain(v =>
            v.Contains("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "ReadResolvedPackages fails with actionable message when restore assets are missing")]
    public void ReadResolvedPackages_ThrowsFileNotFound_WhenAssetsMissing()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var act = () => ReadResolvedPackages(nonExistentPath);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*dotnet restore*");
    }

    [Fact(DisplayName = "BusinessObjects allows both Identity Stores and NetTopologySuite spatial package")]
    public void BusinessObjects_Allows_IdentityStores_And_NetTopologySuite()
    {
        // ARRANGE — Valid package set for BusinessObjects including P2-00 NetTopologySuite
        var validPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.Extensions.Identity.Stores",
            "NetTopologySuite",
        };

        // ACT — Check forbidden prefixes and explicit allow-list
        var forbiddenViolations = FindForbiddenPackages(
            "BusinessObjects", validPackages, ForbiddenBusinessObjectsPackagePrefixes);

        var allowListViolations = validPackages
            .Where(p => !AllowedBusinessObjectsPackages.Contains(p))
            .Select(p => $"PACKAGE VIOLATION in BusinessObjects: '{p}' is not in the explicit allow-list")
            .ToList();

        var allViolations = forbiddenViolations.Concat(allowListViolations).ToList();

        // ASSERT — Both packages must be permitted without violations
        allViolations.Should().BeEmpty(
            because: "BusinessObjects allows Microsoft.Extensions.Identity.Stores (P1-00) and NetTopologySuite (P2-00) for spatial domain invariants");
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

    [Fact(DisplayName = "BusinessObjects production project has no forbidden package references (F1/F2)")]
    public void BusinessObjects_HasNoForbiddenPackages()
    {
        var root = RepositoryRoot();
        var csprojPath = Path.Combine(
            root, "RoadGuardSystem.BusinessObjects", "RoadGuardSystem.aBusinessObjects.csproj");

        var packages = ReadPackageReferences(csprojPath);
        var violations = FindForbiddenPackages(
            "BusinessObjects", packages, ForbiddenBusinessObjectsPackagePrefixes);

        var allowListViolations = packages
            .Where(p => !AllowedBusinessObjectsPackages.Contains(p))
            .Select(p => $"PACKAGE VIOLATION in BusinessObjects: '{p}' is not in the explicit allow-list")
            .ToList();

        var allViolations = violations.Concat(allowListViolations).ToList();

        allViolations.Should().BeEmpty(
            because: "BusinessObjects must not contain EF Core, persistence, or transport packages per AGENTS.md: " +
                     "'BusinessObjects has no dependency on API, Services, Repositories, DTOs, EF Core, or transport details'");
    }

    [Fact(DisplayName = "BusinessObjects resolved package graph contains no transitive EF Core dependencies (F3)")]
    public void BusinessObjects_ResolvedPackageGraph_HasNoForbiddenDependencies()
    {
        var root = RepositoryRoot();
        var boDir = Path.Combine(root, "RoadGuardSystem.BusinessObjects");

        var resolvedPackages = ReadResolvedPackages(boDir);
        var violations = FindForbiddenPackages(
            "BusinessObjects (resolved)", resolvedPackages, ForbiddenTransitiveBusinessObjectsPackagePrefixes);

        violations.Should().BeEmpty(
            because: "the resolved package graph of BusinessObjects must not contain any transitive EF Core dependencies");
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

    [Fact(DisplayName = "Services production project has no legacy ASP.NET Core Identity package")]
    public void Services_HasNoLegacyAspNetCoreIdentityPackage()
    {
        var root = RepositoryRoot();
        var csprojPath = Path.Combine(
            root, "RoadGuardSystem.Services", "RoadGuardSystem.dServices.csproj");

        var packages = ReadPackageReferences(csprojPath);
        var violations = FindForbiddenPackages(
            "Services", packages, ForbiddenServicesPackagePrefixes);

        violations.Should().BeEmpty(
            because: "the net8.0 Services layer must not carry the obsolete ASP.NET Core Identity 2.x dependency graph");
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
