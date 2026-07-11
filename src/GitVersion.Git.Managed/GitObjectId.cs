// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GitVersion.Git;

/// <summary>
/// Identifies an object stored in the Git object database. The id of an object is the hash
/// of its contents. This type is hash-agnostic: it stores up to 32 bytes together with the
/// hash length, supporting both SHA-1 (20 bytes) and SHA-256 (32 bytes) repositories.
/// </summary>
/// <seealso href="https://git-scm.com/book/en/v2/Git-Internals-Git-Objects"/>
internal struct GitObjectId : IEquatable<GitObjectId>
{
    /// <summary>The length, in bytes, of a SHA-1 object id.</summary>
    public const int Sha1Size = 20;

    /// <summary>The length, in bytes, of a SHA-256 object id.</summary>
    public const int Sha256Size = 32;

    private const string HexDigits = "0123456789abcdef";
    private static readonly byte[] ReverseHexDigits = BuildReverseHexDigits();

    [SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "The inline array is written through its span conversion.")]
    private HashBuffer value;
    private byte length;
    private string? sha;

    [InlineArray(Sha256Size)]
    private struct HashBuffer
    {
        private byte element;
    }

    /// <summary>
    /// Gets a <see cref="GitObjectId"/> which represents an empty <see cref="GitObjectId"/>.
    /// </summary>
    public static GitObjectId Empty => default;

    /// <summary>
    /// Gets the length, in bytes, of the hash represented by this object id. Zero for <see cref="Empty"/>.
    /// </summary>
    public readonly int HashLength => this.length;

    public static bool operator ==(GitObjectId left, GitObjectId right) => left.Equals(right);

    public static bool operator !=(GitObjectId left, GitObjectId right) => !left.Equals(right);

    /// <summary>
    /// Parses a sequence of byte values as a <see cref="GitObjectId"/>.
    /// </summary>
    /// <param name="value">The raw hash bytes. Must be exactly 20 (SHA-1) or 32 (SHA-256) bytes in length.</param>
    /// <returns>A <see cref="GitObjectId"/>.</returns>
    public static GitObjectId Parse(ReadOnlySpan<byte> value)
    {
        if (value.Length is not (Sha1Size or Sha256Size))
        {
            throw new ArgumentException($"The length should be {Sha1Size} or {Sha256Size} bytes, but was {value.Length}.", nameof(value));
        }

        var objectId = default(GitObjectId);
        Span<byte> bytes = objectId.value;
        value.CopyTo(bytes);
        objectId.length = (byte)value.Length;
        return objectId;
    }

    /// <summary>
    /// Parses the hexadecimal representation of a <see cref="GitObjectId"/>.
    /// </summary>
    /// <param name="value">The hexadecimal representation. Must be 40 (SHA-1) or 64 (SHA-256) characters long.</param>
    /// <returns>A <see cref="GitObjectId"/>.</returns>
    public static GitObjectId Parse(ReadOnlySpan<char> value)
    {
        if (value.Length is not (2 * Sha1Size or 2 * Sha256Size))
        {
            throw new ArgumentException($"The length should be {2 * Sha1Size} or {2 * Sha256Size} characters, but was {value.Length}.", nameof(value));
        }

        var objectId = default(GitObjectId);
        Span<byte> bytes = objectId.value;
        var byteCount = value.Length / 2;

        for (var i = 0; i < byteCount; i++)
        {
            var high = GetHexValue(value[2 * i]);
            var low = GetHexValue(value[(2 * i) + 1]);

            bytes[i] = (byte)((high << 4) + low);
        }

        objectId.length = (byte)byteCount;
        return objectId;
    }

    /// <summary>
    /// Parses the hexadecimal representation of a <see cref="GitObjectId"/>.
    /// </summary>
    /// <param name="value">The hexadecimal representation. Must be 40 (SHA-1) or 64 (SHA-256) characters long.</param>
    /// <returns>A <see cref="GitObjectId"/>.</returns>
    public static GitObjectId Parse(string value)
    {
        var objectId = Parse(value.AsSpan());
        objectId.sha = value.ToLowerInvariant();
        return objectId;
    }

    /// <summary>
    /// Parses the ASCII-encoded hexadecimal representation of a <see cref="GitObjectId"/>.
    /// </summary>
    /// <param name="value">The hexadecimal representation, encoded in ASCII. Must be 40 (SHA-1) or 64 (SHA-256) bytes long.</param>
    /// <returns>A <see cref="GitObjectId"/>.</returns>
    public static GitObjectId ParseHex(ReadOnlySpan<byte> value)
    {
        if (value.Length is not (2 * Sha1Size or 2 * Sha256Size))
        {
            throw new ArgumentException($"The length should be {2 * Sha1Size} or {2 * Sha256Size} characters, but was {value.Length}.", nameof(value));
        }

        var objectId = default(GitObjectId);
        Span<byte> bytes = objectId.value;
        var byteCount = value.Length / 2;

        for (var i = 0; i < byteCount; i++)
        {
            var high = GetHexValue((char)value[2 * i]);
            var low = GetHexValue((char)value[(2 * i) + 1]);

            bytes[i] = (byte)((high << 4) + low);
        }

        objectId.length = (byte)byteCount;
        return objectId;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is GitObjectId other && Equals(other);

    /// <inheritdoc/>
    public readonly bool Equals(GitObjectId other)
    {
        ReadOnlySpan<byte> mine = this.value;
        ReadOnlySpan<byte> theirs = other.value;
        return this.length == other.length && mine.SequenceEqual(theirs);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        ReadOnlySpan<byte> bytes = this.value;
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    /// <summary>
    /// Gets a <see cref="ushort"/> which represents the first two bytes of this <see cref="GitObjectId"/>.
    /// </summary>
    /// <returns>The first two bytes of this <see cref="GitObjectId"/>, in big-endian order.</returns>
    public readonly ushort AsUInt16()
    {
        ReadOnlySpan<byte> bytes = this.value;
        return BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    /// <summary>
    /// Copies the byte representation of this <see cref="GitObjectId"/> to a <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="destination">The memory to which to copy this <see cref="GitObjectId"/>. Must be at least <see cref="HashLength"/> bytes long.</param>
    public readonly void CopyTo(Span<byte> destination)
    {
        ReadOnlySpan<byte> bytes = this.value;
        bytes[..this.length].CopyTo(destination);
    }

    /// <summary>
    /// Returns the hexadecimal representation of this object id.
    /// </summary>
    /// <inheritdoc/>
    public override string ToString() => this.sha ??= ToString(this.length * 2);

    /// <summary>
    /// Returns the first <paramref name="length"/> hexadecimal characters of this object id.
    /// </summary>
    /// <param name="length">The number of hexadecimal characters to return.</param>
    /// <returns>A hexadecimal prefix of this object id.</returns>
    public readonly string ToString(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, this.length * 2);

        Span<char> chars = stackalloc char[length];
        ReadOnlySpan<byte> bytes = this.value;

        for (var i = 0; i < chars.Length; i++)
        {
            var b = bytes[i >> 1];
            chars[i] = HexDigits[(i & 1) == 0 ? b >> 4 : b & 0x0F];
        }

        return new string(chars);
    }

    private static int GetHexValue(char c)
    {
        var index = c - '0';
        if (index < 0 || index >= ReverseHexDigits.Length || (index != 0 && ReverseHexDigits[index] == 0 && c != '0'))
        {
            throw new FormatException($"The character '{c}' is not a valid hexadecimal digit.");
        }

        return ReverseHexDigits[index];
    }

    private static byte[] BuildReverseHexDigits()
    {
        var bytes = new byte['f' - '0' + 1];

        for (var i = 0; i < 10; i++)
        {
            bytes[i] = (byte)i;
        }

        for (var i = 10; i < 16; i++)
        {
            bytes[i + 'a' - '0' - 0x0a] = (byte)i;
            bytes[i + 'A' - '0' - 0x0a] = (byte)i;
        }

        return bytes;
    }
}
