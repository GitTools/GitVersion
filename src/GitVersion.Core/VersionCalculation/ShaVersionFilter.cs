using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

internal class ShaVersionFilter : IVersionFilter
{
    private readonly IEnumerable<string> shaList;

    public ShaVersionFilter(IEnumerable<string> shaList) => this.shaList = shaList.NotNull();

    public bool Exclude(BaseVersion? version, [NotNullWhen(true)] out string? reason)
    {
        version.NotNull();

        reason = null;

        if (version.BaseVersionSource == null || !this.shaList.Any(sha => version.BaseVersionSource.Sha.StartsWith(sha, StringComparison.OrdinalIgnoreCase)))
            return false;

        reason = $"Sha {version.BaseVersionSource} was ignored due to commit having been excluded by configuration";
        return true;
    }
}
