using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public class ShaVersionFilter : IVersionFilter
{
    private readonly IEnumerable<string> shas;

    public ShaVersionFilter(IEnumerable<string> shas) => this.shas = shas.NotNull();

    public bool Exclude(BaseVersion version, [NotNullWhen(true)] out string? reason)
    {
        if (version == null) throw new ArgumentNullException(nameof(version));

        reason = null;

        if (version.BaseVersionSource == null || !this.shas.Any(sha => version.BaseVersionSource.Sha.StartsWith(sha, StringComparison.OrdinalIgnoreCase)))
            return false;

        reason = $"Sha {version.BaseVersionSource} was ignored due to commit having been excluded by configuration";
        return true;

    }
}
