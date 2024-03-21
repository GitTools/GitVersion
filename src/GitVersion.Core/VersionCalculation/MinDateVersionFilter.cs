using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

internal class MinDateVersionFilter(DateTimeOffset minimum) : IVersionFilter
{
    public bool Exclude(IBaseVersion baseVersion, [NotNullWhen(true)] out string? reason)
    {
        baseVersion.NotNull();

        reason = null;

        if (baseVersion.BaseVersionSource == null || baseVersion.BaseVersionSource.When >= minimum)
            return false;

        reason = "Source was ignored due to commit date being outside of configured range";
        return true;
    }
}
