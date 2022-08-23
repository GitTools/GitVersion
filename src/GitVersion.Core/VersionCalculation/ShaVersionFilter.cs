using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public class ShaVersionFilter : IVersionFilter
{
    private readonly IEnumerable<string> shas;

    public ShaVersionFilter(IEnumerable<string> shas) => this.shas = shas.NotNull();

    public bool Exclude(ICommit commit, out string? reason)
    {
        commit.NotNull();

        reason = null;

        if (!this.shas.Any(sha => commit.Sha.StartsWith(sha, StringComparison.OrdinalIgnoreCase)))
            return false;

        reason = $"Sha {commit} was ignored due to commit having been excluded by configuration";
        return true;
    }
}
