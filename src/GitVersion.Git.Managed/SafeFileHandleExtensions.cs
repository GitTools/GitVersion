using Microsoft.Win32.SafeHandles;

namespace GitVersion.Git;

/// <summary>
/// Provides extension methods for reading files through a <see cref="SafeFileHandle"/>.
/// </summary>
internal static class SafeFileHandleExtensions
{
    /// <summary>
    /// Fills <paramref name="buffer"/> with the bytes stored at <paramref name="offset"/> in the file,
    /// without affecting any stream position.
    /// </summary>
    /// <param name="handle">The handle of the file to read from.</param>
    /// <param name="offset">The offset in the file at which to start reading.</param>
    /// <param name="buffer">The buffer to fill.</param>
    /// <exception cref="EndOfStreamException">Thrown when the file ends before <paramref name="buffer"/> could be filled.</exception>
    public static void ReadExactlyAt(this SafeFileHandle handle, long offset, Span<byte> buffer)
    {
        var totalRead = 0;

        while (totalRead < buffer.Length)
        {
            var read = RandomAccess.Read(handle, buffer[totalRead..], offset + totalRead);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            totalRead += read;
        }
    }
}
