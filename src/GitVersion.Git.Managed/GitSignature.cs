// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

using System.Buffers.Text;

namespace GitVersion.Git;

/// <summary>
/// Represents the signature of a Git committer, author or tagger.
/// </summary>
/// <param name="Name">The name of the person.</param>
/// <param name="Email">The e-mail address of the person.</param>
/// <param name="When">The date and time of the action, including the recorded UTC offset.</param>
internal readonly record struct GitSignature(string Name, string Email, DateTimeOffset When)
{
    /// <summary>
    /// Parses a signature line value in the format <c>Name &lt;email&gt; timestamp offset</c>,
    /// for example <c>Jane Doe &lt;jane@example.com&gt; 1580753429 +0100</c>.
    /// </summary>
    /// <param name="value">The signature value (excluding the <c>author</c>/<c>committer</c>/<c>tagger</c> prefix).</param>
    /// <param name="encodingName">The value of the containing object's <c>encoding</c> header, if present.</param>
    /// <returns>The parsed <see cref="GitSignature"/>.</returns>
    public static GitSignature Parse(ReadOnlySpan<byte> value, string? encodingName = null)
    {
        var emailStart = value.IndexOf((byte)'<');
        var emailEnd = value.IndexOf((byte)'>');

        if (emailStart < 0 || emailEnd < emailStart)
        {
            throw new GitObjectStoreException("A signature line is malformed: no e-mail address found.");
        }

        var name = value[..Math.Max(emailStart - 1, 0)];
        var email = value[(emailStart + 1)..emailEnd];
        var time = value[Math.Min(emailEnd + 2, value.Length)..];

        var offsetStart = time.IndexOf((byte)' ');
        if (offsetStart < 0)
        {
            offsetStart = time.Length;
        }

        if (!Utf8Parser.TryParse(time[..offsetStart], out long seconds, out _))
        {
            throw new GitObjectStoreException("A signature line is malformed: no timestamp found.");
        }

        var when = DateTimeOffset.FromUnixTimeSeconds(seconds);

        if (TryParseOffset(time[Math.Min(offsetStart + 1, time.Length)..], out var offset))
        {
            when = when.ToOffset(offset);
        }

        return new(GitTextDecoder.Decode(name, encodingName), GitTextDecoder.Decode(email, encodingName), when);
    }

    private static bool TryParseOffset(ReadOnlySpan<byte> value, out TimeSpan offset)
    {
        offset = TimeSpan.Zero;

        if (value.Length < 5 || (value[0] != '+' && value[0] != '-'))
        {
            return false;
        }

        var hours = ((value[1] - '0') * 10) + (value[2] - '0');
        var minutes = ((value[3] - '0') * 10) + (value[4] - '0');

        if (hours is < 0 or > 14 || minutes is < 0 or > 59)
        {
            return false;
        }

        offset = new TimeSpan(hours, minutes, 0);

        if (value[0] == '-')
        {
            offset = offset.Negate();
        }

        return true;
    }
}
