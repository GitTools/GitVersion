// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Diagnostics.CodeAnalysis;

namespace GitVersion.Git;

internal static class FileHelpers
{
    /// <summary>
    /// Opens the file with a given path for reading, if it exists.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="stream">The stream opened over the file, if the file exists.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise <see langword="false"/>.</returns>
    public static bool TryOpen(string path, [NotNullWhen(true)] out FileStream? stream)
    {
        if (!File.Exists(path))
        {
            stream = null;
            return false;
        }

        try
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return true;
        }
        catch (IOException)
        {
            stream = null;
            return false;
        }
    }
}
