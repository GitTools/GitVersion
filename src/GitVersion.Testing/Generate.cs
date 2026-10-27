using GitVersion.Testing.Git;

namespace GitVersion.Testing;

/// <summary>
/// Static helper class for generating data git needs, like signatures
/// </summary>
public static class Generate
{
    /// <summary>
    /// Create a Git signature at VirtualTime.Now
    /// </summary>
    public static Signature SignatureNow()
    {
        var dateTimeOffset = VirtualTime.Now;
        return Signature(dateTimeOffset);
    }

    /// <summary>
    /// Creates a Git signature at the specified time
    /// </summary>
    private static Signature Signature(DateTimeOffset dateTimeOffset) => new("A. U. Thor", "thor@valhalla.asgard.com", dateTimeOffset);
}
