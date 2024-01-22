using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.Core;

internal sealed class BranchRepository(Lazy<GitVersionContext> versionContext, IGitRepository gitRepository)
    : IBranchRepository
{
    private GitVersionContext VersionContext => this.versionContextLazy.Value;
    private readonly Lazy<GitVersionContext> versionContextLazy = versionContext.NotNull();

    private readonly IGitRepository gitRepository = gitRepository.NotNull();

    public IEnumerable<IBranch> GetMainlineBranches(params IBranch[] excludeBranches)
        => GetBranches([.. excludeBranches], configuration => configuration.IsMainline == true);

    public IEnumerable<IBranch> GetReleaseBranches(params IBranch[] excludeBranches)
        => GetBranches([.. excludeBranches], configuration => configuration.IsReleaseBranch == true);

    private IEnumerable<IBranch> GetBranches(HashSet<IBranch> excludeBranches, Func<IBranchConfiguration, bool> predicate)
    {
        predicate.NotNull();

        foreach (var branch in this.gitRepository.Branches)
        {
            if (!excludeBranches.Contains(branch))
            {
                var branchConfiguration = VersionContext.Configuration.GetBranchConfiguration(branch.Name);
                if (predicate(branchConfiguration))
                {
                    yield return branch;
                }
            }
        }
    }
}
