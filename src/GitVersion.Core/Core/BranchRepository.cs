using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.Core;

internal sealed class BranchRepository(IRepositoryStore repositoryStore) : IBranchRepository
{
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();

    public IEnumerable<IBranch> GetMainBranches(IGitVersionConfiguration configuration, params IBranch[] excludeBranches)
        => GetBranches(configuration, [.. excludeBranches], branchConfiguration => branchConfiguration.IsMainBranch == true);

    public IEnumerable<IBranch> GetReleaseBranches(IGitVersionConfiguration configuration, params IBranch[] excludeBranches)
        => GetBranches(configuration, [.. excludeBranches], branchConfiguration => branchConfiguration.IsReleaseBranch == true);

    private IEnumerable<IBranch> GetBranches(
        IGitVersionConfiguration configuration, HashSet<IBranch> excludeBranches, Func<IBranchConfiguration, bool> predicate)
    {
        predicate.NotNull();

        foreach (var branch in this.repositoryStore.Branches)
        {
            if (excludeBranches.Contains(branch)) continue;
            var branchConfiguration = configuration.GetBranchConfiguration(branch.Name);
            if (predicate(branchConfiguration))
            {
                yield return branch;
            }
        }
    }
}
