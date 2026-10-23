using System.IO.Abstractions;

namespace GitVersion.Telemetry;

/// <summary>Discloses the policy once per local marker without storing identities or event data.</summary>
internal sealed class TelemetryNotice(IFileSystem fileSystem, Func<string> localDataDirectory, Action<string> write)
{
    // Bump the marker when a materially changed policy needs a new disclosure.
    private const string MarkerName = "telemetry-policy-v2";

    /// <summary>Writes the disclosure before creating an empty marker; state failures allow later redisclosure.</summary>
    /// <param name="message">The policy disclosure to write when no marker exists.</param>
    /// <remarks>A writer failure propagates so the reporter skips transmission after failed disclosure.</remarks>
    internal void Show(string message)
    {
        string? marker = null;
        try
        {
            var directory = localDataDirectory();
            if (!string.IsNullOrEmpty(directory))
            {
                marker = fileSystem.Path.Combine(directory, "GitVersion", MarkerName);
                if (fileSystem.File.Exists(marker))
                {
                    return;
                }
            }
        }
        catch (Exception)
        {
            // An unavailable home/state directory must not suppress disclosure.
            marker = null;
        }

        // Mark only after successful disclosure. A failure here prevents the send.
        write(message);
        if (marker == null)
        {
            return;
        }
        try
        {
            fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(marker)!);
            using var stream = fileSystem.FileStream.New(marker, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        }
        catch (Exception)
        {
            // Read-only homes and concurrent marker creation are harmless. The notice may repeat.
            return;
        }
    }
}
