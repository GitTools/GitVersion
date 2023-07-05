using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

internal class MinDateVersionFilter : IVersionFilter
{
    private readonly DateTimeOffset minimum;

    public MinDateVersionFilter(DateTimeOffset minimum) => this.minimum = minimum;

    public bool Exclude(BaseVersion? version, [NotNullWhen(true)] out string? reason)
    {
        version.NotNull();

        reason = null;

        if (version.BaseVersionSource == null || version.BaseVersionSource.When >= this.minimum)
            return false;

        reason = "Source was ignored due to commit date being outside of configured range";
        return true;
    }
}
