using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

internal class MinDateVersionFilter(DateTimeOffset minimum) : IVersionFilter
{
    public bool Exclude(BaseVersion? version, [NotNullWhen(true)] out string? reason)
    {
        version.NotNull();

        reason = null;

        if (version.BaseVersionSource == null || version.BaseVersionSource.When >= minimum)
            return false;

        reason = "Source was ignored due to commit date being outside of configured range";
        return true;
    }
}
