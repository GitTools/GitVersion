namespace GitVersion.Core;

internal interface IBranchRepository
{
    IEnumerable<IBranch> GetMainBranches(params IBranch[] excludeBranches);

    IEnumerable<IBranch> GetReleaseBranches(params IBranch[] excludeBranches);
}
