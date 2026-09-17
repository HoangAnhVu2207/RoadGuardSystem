using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace RoadGuardSystem.UnitTests.Architecture;

/// <summary>
/// Dependency-graph checker: reads ProjectReference elements from .csproj files
/// and validates that no forbidden dependency edge exists.
/// No third-party architecture-testing package is used because the graph is a
/// simple directed-graph reachability check that can be implemented cleanly with
/// XDocument + a DFS cycle detector.
/// </summary>
public static class DependencyGraphChecker
{
    /// <summary>
    /// Represents a directed dependency graph where each key depends on its value set.
    /// Keys and values are short assembly/project names (e.g. "API", "Services").
    /// </summary>
    public sealed record DependencyGraph(IReadOnlyDictionary<string, IReadOnlySet<string>> Edges);

    /// <summary>
    /// Checks whether <paramref name="from"/> can reach <paramref name="to"/>
    /// (directly or transitively) in the graph.
    /// </summary>
    public static bool CanReach(DependencyGraph graph, string from, string to)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return Dfs(from);

        bool Dfs(string current)
        {
            if (!visited.Add(current)) return false;
            if (!graph.Edges.TryGetValue(current, out var neighbors)) return false;
            foreach (var n in neighbors)
            {
                if (string.Equals(n, to, StringComparison.OrdinalIgnoreCase)) return true;
                if (Dfs(n)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Detects a cycle anywhere in the graph using DFS coloring.
    /// Returns the first cycle path found, or null if none.
    /// </summary>
    public static string? FindCycle(DependencyGraph graph)
    {
        var color = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 0=white,1=gray,2=black
        var path = new Stack<string>();

        foreach (var node in graph.Edges.Keys)
        {
            if (!color.ContainsKey(node) || color[node] == 0)
            {
                var result = DfsColor(node);
                if (result is not null) return result;
            }
        }
        return null;

        string? DfsColor(string current)
        {
            color[current] = 1;
            path.Push(current);
            if (graph.Edges.TryGetValue(current, out var neighbors))
            {
                foreach (var n in neighbors)
                {
                    if (!color.ContainsKey(n)) color[n] = 0;
                    if (color[n] == 1)
                    {
                        // Found cycle — reconstruct path
                        var cycle = path.TakeWhile(x => !string.Equals(x, n, StringComparison.OrdinalIgnoreCase)).Reverse().ToList();
                        cycle.Add(n);
                        cycle.Add(n);
                        return string.Join(" -> ", cycle);
                    }
                    if (color[n] == 0)
                    {
                        var result = DfsColor(n);
                        if (result is not null) return result;
                    }
                }
            }
            path.Pop();
            color[current] = 2;
            return null;
        }
    }

    /// <summary>
    /// Reads the direct ProjectReference names from a given .csproj file.
    /// Returns the assembly names referenced (derived from the Include path filename).
    /// </summary>
    public static IReadOnlySet<string> ReadDirectReferences(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        var refs = doc.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .Select(v => Path.GetFileNameWithoutExtension(v!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return refs;
    }

    /// <summary>
    /// Reads the PackageReference Include attribute values from a given .csproj file.
    /// Returns package IDs (case-insensitive).
    /// </summary>
    public static IReadOnlySet<string> ReadPackageReferences(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        var packages = doc.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .Select(v => v!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return packages;
    }

    /// <summary>
    /// Builds a DependencyGraph from projects and reference reader, mapping assembly names to logical names.
    /// Preserves unmapped project references with their raw names instead of silently dropping them.
    /// </summary>
    public static DependencyGraph BuildGraph(
        IReadOnlyDictionary<string, string> projects,
        IReadOnlyDictionary<string, string> assemblyToLogical,
        Func<string, IReadOnlySet<string>> referenceReader)
    {
        var edges = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (logical, path) in projects)
        {
            var rawRefs = referenceReader(path);
            var logicalRefs = rawRefs
                .Select(r => assemblyToLogical.TryGetValue(r, out var mapped) ? mapped : r)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            edges[logical] = logicalRefs;
        }

        return new DependencyGraph(edges);
    }

    /// <summary>
    /// Parses resolved packages from project.assets.json content.
    /// Extracts resolved package identifiers while excluding project references.
    /// </summary>
    public static IReadOnlySet<string> ParseResolvedPackages(string assetsJsonContent)
    {
        using var doc = JsonDocument.Parse(assetsJsonContent);
        var root = doc.RootElement;
        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("targets", out var targets))
        {
            foreach (var target in targets.EnumerateObject())
            {
                foreach (var item in target.Value.EnumerateObject())
                {
                    if (item.Value.TryGetProperty("type", out var typeProp) &&
                        string.Equals(typeProp.GetString(), "project", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var name = item.Name;
                    var slash = name.IndexOf('/');
                    packages.Add(slash >= 0 ? name[..slash] : name);
                }
            }
        }

        if (root.TryGetProperty("libraries", out var libraries))
        {
            foreach (var lib in libraries.EnumerateObject())
            {
                if (lib.Value.TryGetProperty("type", out var typeProp) &&
                    string.Equals(typeProp.GetString(), "project", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var name = lib.Name;
                var slash = name.IndexOf('/');
                packages.Add(slash >= 0 ? name[..slash] : name);
            }
        }

        return packages;
    }

    /// <summary>
    /// Reads resolved packages from project.assets.json in the project's obj directory.
    /// Throws FileNotFoundException with an actionable message if restore assets do not exist.
    /// </summary>
    public static IReadOnlySet<string> ReadResolvedPackages(string projectDirectoryOrAssetsPath)
    {
        string assetsPath = projectDirectoryOrAssetsPath.EndsWith("project.assets.json", StringComparison.OrdinalIgnoreCase)
            ? projectDirectoryOrAssetsPath
            : Path.Combine(projectDirectoryOrAssetsPath, "obj", "project.assets.json");

        if (!File.Exists(assetsPath))
        {
            throw new FileNotFoundException(
                $"Restore assets file '{assetsPath}' was not found. Run 'dotnet restore' first to generate resolved dependency assets.",
                assetsPath);
        }

        var json = File.ReadAllText(assetsPath);
        return ParseResolvedPackages(json);
    }

    // ---------------------------------------------------------------------------
    // Package-level boundary rules
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Package prefixes that must NOT appear in BusinessObjects.
    /// BusinessObjects must not depend on EF Core, EF Identity, or transport packages.
    /// AGENTS.md: "BusinessObjects has no dependency on API, Services, Repositories,
    /// DTOs, EF Core, or transport details."
    /// </summary>
    public static readonly IReadOnlyList<string> ForbiddenBusinessObjectsPackagePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "System.IdentityModel",
        "Microsoft.IdentityModel",
        "System.Net.Http",
    ];

    /// <summary>
    /// Explicit allow-list of permitted direct packages in BusinessObjects.
    /// P1-00 owns Microsoft.Extensions.Identity.Stores (pure Identity models without EF).
    /// P2-00 owns NetTopologySuite (spatial geometry/geography domain primitives and invariants).
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedBusinessObjectsPackages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.Extensions.Identity.Stores",
            "NetTopologySuite", // P2-00: spatial domain models and geometry calculations
        };

    /// <summary>
    /// Package prefixes that must NOT appear transitively in BusinessObjects resolved assets.
    /// </summary>
    public static readonly IReadOnlyList<string> ForbiddenTransitiveBusinessObjectsPackagePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
    ];

    /// <summary>
    /// Package prefixes that must NOT appear in DTOs (as non-private assets).
    /// DTOs own public contracts only; persistence and HTTP infrastructure belong in
    /// Repositories or API.
    /// </summary>
    public static readonly IReadOnlyList<string> ForbiddenDTOsPackagePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore.Http",
        "Microsoft.Extensions.Configuration",
        "Microsoft.Extensions.Hosting",
        "Microsoft.Extensions.Options",
    ];

    /// <summary>
    /// Legacy ASP.NET Core Identity packages must not be referenced by Services in
    /// this net8.0 solution. Identity model abstractions live in BusinessObjects.
    /// </summary>
    public static readonly IReadOnlyList<string> ForbiddenServicesPackagePrefixes =
    [
        "Microsoft.AspNetCore.Identity",
    ];

    /// <summary>
    /// Checks a set of package IDs against a list of forbidden prefixes.
    /// Returns matching (packageId, prefix) pairs.
    /// </summary>
    public static IReadOnlyList<string> FindForbiddenPackages(
        string projectLogicalName,
        IReadOnlySet<string> packages,
        IReadOnlyList<string> forbiddenPrefixes)
    {
        var violations = new List<string>();
        foreach (var pkg in packages)
        {
            foreach (var prefix in forbiddenPrefixes)
            {
                if (pkg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add(
                        $"PACKAGE VIOLATION in {projectLogicalName}: '{pkg}' matches forbidden prefix '{prefix}'");
                    break;
                }
            }
        }
        return violations;
    }

    /// <summary>
    /// Builds the forbidden-edge checker matrix and verifies none are violated.
    ///
    /// Most rules use TRANSITIVE reachability (if A can reach B through any path, it is a violation).
    /// Exception: "API must not depend DIRECTLY on Repositories" — this checks only the
    /// immediate ProjectReference list, not transitive paths, because API → Services → Repositories
    /// is the intended (and allowed) transitive chain.
    ///
    /// Returns a list of violation descriptions; empty means no violations.
    /// </summary>
    public static IReadOnlyList<string> FindForbiddenEdges(DependencyGraph graph)
    {
        // Forbidden TRANSITIVE paths (from cannot reach to, directly or indirectly)
        var transitivelyForbidden = new (string From, string To, string Rule)[]
        {
            // BusinessObjects must be a pure domain layer — no upward dependencies.
            ("BusinessObjects", "DTOs",         "BusinessObjects must not depend on DTOs"),
            ("BusinessObjects", "Repositories", "BusinessObjects must not depend on Repositories"),
            ("BusinessObjects", "Services",     "BusinessObjects must not depend on Services"),
            ("BusinessObjects", "API",          "BusinessObjects must not depend on API"),
            // DTOs own contracts only — no persistence or API dependencies.
            ("DTOs",            "Repositories", "DTOs must not depend on Repositories"),
            ("DTOs",            "Services",     "DTOs must not depend on Services"),
            ("DTOs",            "API",          "DTOs must not depend on API"),
            // Lower layers must not reference higher layers.
            ("Repositories",    "Services",     "Repositories must not depend on Services"),
            ("Repositories",    "API",          "Repositories must not depend on API"),
            ("Services",        "API",          "Services must not depend on API"),
        };

        var violations = new List<string>();
        foreach (var (from, to, rule) in transitivelyForbidden)
        {
            if (CanReach(graph, from, to))
                violations.Add($"VIOLATION: {rule} — path found from '{from}' to '{to}'");
        }

        // Domain boundary: BusinessObjects must not depend on ANY project.
        if (graph.Edges.TryGetValue("BusinessObjects", out var boDeps))
        {
            foreach (var dep in boDeps)
            {
                var alreadyReported = transitivelyForbidden.Any(t =>
                    string.Equals(t.From, "BusinessObjects", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(t.To, dep, StringComparison.OrdinalIgnoreCase));
                if (!alreadyReported)
                {
                    violations.Add(
                        $"VIOLATION: BusinessObjects must not depend on '{dep}' — BusinessObjects must have zero project references");
                }
            }
        }

        // Architecture boundary: All project references must be known mapped architecture layers.
        var knownLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BusinessObjects", "DTOs", "Repositories", "Services", "API",
        };
        foreach (var (from, neighbors) in graph.Edges)
        {
            foreach (var to in neighbors)
            {
                if (!knownLayers.Contains(to))
                {
                    violations.Add(
                        $"VIOLATION: Project '{from}' references unmapped project '{to}' — unknown production reference");
                }
            }
        }

        // Forbidden DIRECT edge only: API must not directly reference Repositories.
        // Transitive reach (API -> Services -> Repositories) is allowed and expected.
        if (graph.Edges.TryGetValue("API", out var apiDeps) &&
            apiDeps.Contains("Repositories", StringComparer.OrdinalIgnoreCase))
        {
            violations.Add(
                "VIOLATION: API must not depend directly on Repositories — direct ProjectReference found");
        }

        var cycle = FindCycle(graph);
        if (cycle is not null)
            violations.Add($"CIRCULAR DEPENDENCY detected: {cycle}");

        return violations;
    }
}
