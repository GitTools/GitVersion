// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Buffers;

namespace GitVersion.Git;

/// <summary>
/// Provides extension methods for the <see cref="Stream"/> class.
/// </summary>
internal static class StreamExtensions
{
    /// <summary>
    /// Reads a variable-length integer off a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The stream off which to read the variable-length integer.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref="EndOfStreamException">Thrown when the stream runs out of data before the integer could be read.</exception>
    public static int ReadMbsInt(this Stream stream)
    {
        var value = 0;
        var currentBit = 0;

        while (true)
        {
            var read = stream.ReadByte();
            if (read == -1)
            {
                throw new EndOfStreamException();
            }

            value |= (read & 0b_0111_1111) << currentBit;
            currentBit += 7;

            if (read < 128)
            {
                break;
            }
        }

        return value;
    }

    /// <summary>
    /// Reads the specified number of bytes from a stream, or until the end of the stream.
    /// </summary>
    /// <param name="readFrom">The stream to read from.</param>
    /// <param name="length">The number of bytes to be read.</param>
    /// <param name="copyTo">The stream to copy the read bytes to, if required.</param>
    /// <returns>The number of bytes actually read. This will be less than <paramref name="length"/> only if the end of <paramref name="readFrom"/> is reached.</returns>
    public static int ReadBytes(this Stream readFrom, int length, Stream? copyTo = null)
    {
        var bytesRemaining = length;
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(50 * 1024, bytesRemaining));
        while (bytesRemaining > 0)
        {
            var read = readFrom.Read(buffer, 0, Math.Min(buffer.Length, bytesRemaining));
            if (read == 0)
            {
                break;
            }

            copyTo?.Write(buffer, 0, read);
            bytesRemaining -= read;
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return length - bytesRemaining;
    }
}
