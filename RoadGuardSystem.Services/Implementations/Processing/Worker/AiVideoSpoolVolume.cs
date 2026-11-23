namespace RoadGuardSystem.Services.Processing.Worker;

public static class AiVideoSpoolVolume
{
    public static string ResolvePath(string spoolRoot, IEnumerable<string> mountedRoots)
    {
        var path = Path.GetFullPath(spoolRoot);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        // Unix spools can live on a nested mount with a different capacity from '/'.
        return mountedRoots.Select(root => Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)))
            .Where(root => path.Equals(root, comparison) || path.StartsWith(
                Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar, comparison))
            .OrderByDescending(root => root.Length)
            .FirstOrDefault() ?? throw new IOException("TEMP_STORAGE_VOLUME_UNAVAILABLE");
    }
}
