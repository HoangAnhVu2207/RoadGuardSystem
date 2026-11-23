using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace RoadGuardSystem.Services.Processing.Worker;

public sealed record AiWorkerVideoMetadata(int Width, int Height, int DurationMs);
public interface IAiWorkerMediaProbe
{
    Task<AiWorkerVideoMetadata> ProbeAsync(string localFile, CancellationToken cancellationToken);
}

/// <summary>Reads actual media metadata; never infers duration from frame count or nominal FPS.</summary>
public sealed class AiWorkerMediaProbe(string ffprobePath) : IAiWorkerMediaProbe
{
    public async Task<AiWorkerVideoMetadata> ProbeAsync(string localFile, CancellationToken cancellationToken)
    {
        if (!Path.IsPathFullyQualified(localFile)) throw new ArgumentException("A staged local media file is required.", nameof(localFile));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); deadline.CancelAfter(TimeSpan.FromSeconds(45));
        var start = new ProcessStartInfo(ffprobePath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-v", "error", "-select_streams", "v", "-show_entries", "stream=width,height:format=duration", "-of", "json", localFile }) start.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = start };
        var started = false;
        try
        {
            if (!process.Start()) throw new IOException("MEDIA_PROBE_UNAVAILABLE");
            started = true;
            var output = ReadBoundedAsync(process.StandardOutput, 16 * 1024, deadline.Token);
            var errors = ReadBoundedAsync(process.StandardError, 4 * 1024, deadline.Token);
            await Task.WhenAll(output, errors, process.WaitForExitAsync(deadline.Token));
            if (process.ExitCode != 0) throw new InvalidDataException("INVALID_VIDEO_METADATA");
            return Parse(await output);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new IOException("MEDIA_PROBE_TIMEOUT"); }
        catch (System.ComponentModel.Win32Exception) { throw new IOException("MEDIA_PROBE_UNAVAILABLE"); }
        finally { if (started && !process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(CancellationToken.None); } }
    }
    private static async Task<string> ReadBoundedAsync(StreamReader reader, int maximum, CancellationToken ct)
    {
        var result = new StringBuilder(); var buffer = new char[1024]; int read;
        while ((read = await reader.ReadAsync(buffer, ct)) != 0)
        {
            if (result.Length + read > maximum) throw new InvalidDataException("MEDIA_PROBE_OUTPUT_LIMIT"); result.Append(buffer, 0, read);
        }
        return result.ToString();
    }
    public static AiWorkerVideoMetadata Parse(string output)
    {
        try
        {
            using var document = JsonDocument.Parse(output);
            var streams = document.RootElement.GetProperty("streams");
            if (streams.GetArrayLength() != 1) throw new InvalidDataException("INVALID_VIDEO_METADATA");
            var width = streams[0].GetProperty("width").GetInt32(); var height = streams[0].GetProperty("height").GetInt32();
            var duration = document.RootElement.GetProperty("format").GetProperty("duration").GetString();
            if (!decimal.TryParse(duration, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds) ||
                width is < 1 or > 16384 || height is < 1 or > 16384 || seconds <= 0 || seconds > int.MaxValue / 1000m)
                throw new InvalidDataException("INVALID_VIDEO_METADATA");
            var ms = AiWorkerCoordinates.TimestampMilliseconds(seconds);
            if (ms < 1) throw new InvalidDataException("INVALID_VIDEO_METADATA");
            return new(width, height, ms);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        { throw new InvalidDataException("INVALID_VIDEO_METADATA"); }
    }
}
