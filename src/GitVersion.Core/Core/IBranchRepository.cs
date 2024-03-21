using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.Core;

internal interface IBranchRepository
{
    IEnumerable<IBranch> GetMainBranches(IGitVersionConfiguration configuration, params IBranch[] excludeBranches);

    IEnumerable<IBranch> GetReleaseBranches(IGitVersionConfiguration configuration, params IBranch[] excludeBranches);
}
