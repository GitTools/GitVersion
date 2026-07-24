namespace GitVersion.Git;

/// <summary>
/// Decodes text stored in Git objects, matching git's behavior: content is assumed to be
/// UTF-8 (or the encoding named by the commit's <c>encoding</c> header), falling back to
/// Latin-1 when the bytes are not valid UTF-8.
/// </summary>
internal static class GitTextDecoder
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Decodes the given bytes as UTF-8, falling back to Latin-1 when the bytes are not valid UTF-8.
    /// </summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <returns>The decoded string.</returns>
    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    /// <summary>
    /// Decodes the given bytes using the named encoding when available, otherwise
    /// falling back to <see cref="Decode(ReadOnlySpan{byte})"/>.
    /// </summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <param name="encodingName">The value of the object's <c>encoding</c> header, if present.</param>
    /// <returns>The decoded string.</returns>
    public static string Decode(ReadOnlySpan<byte> bytes, string? encodingName)
    {
        if (encodingName is not null && !encodingName.Equals("UTF-8", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return Encoding.GetEncoding(encodingName).GetString(bytes);
            }
            catch (ArgumentException)
            {
                // Unknown or unsupported encoding: fall back to git's default behavior.
            }
        }

        return Decode(bytes);
    }
}
