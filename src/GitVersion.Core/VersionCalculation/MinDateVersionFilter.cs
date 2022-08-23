using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public class MinDateVersionFilter : IVersionFilter
{
    private readonly DateTimeOffset minimum;

    public MinDateVersionFilter(DateTimeOffset minimum) => this.minimum = minimum;

    public bool Exclude(ICommit commit, out string? reason)
    {
        commit.NotNull();

        reason = null;

        if (commit.When >= this.minimum)
            return false;

        reason = $"Source {commit} was ignored due to commit date being outside of configured range";
        return true;
    }
}
