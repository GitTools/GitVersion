using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

internal class ShaVersionFilter(IEnumerable<string> shaList) : IVersionFilter
{
    private readonly IEnumerable<string> shaList = shaList.NotNull();

    public bool Exclude(IBaseVersion baseVersion, [NotNullWhen(true)] out string? reason)
    {
        baseVersion.NotNull();

        reason = null;

        if (baseVersion.BaseVersionSource == null
            || !this.shaList.Any(sha => baseVersion.BaseVersionSource.Sha.StartsWith(sha, StringComparison.OrdinalIgnoreCase)))
            return false;

        reason = $"Sha {baseVersion.BaseVersionSource} was ignored due to commit having been excluded by configuration";
        return true;
    }
}
